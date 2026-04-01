using EtimadScraper.Configuration;
using EtimadScraper.Data;
using EtimadScraper.Entities;
using EtimadScraper.Models;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace EtimadScraper.Services;

/// <summary>
/// Fetches all tenders from the Etimad supplier-tenders JSON API and
/// upserts them into the <c>SupplierTenders</c> SQL Server table.
///
/// Key behaviours:
/// • Uses <see cref="IHttpClientFactory"/> (named client "EtimadClient").
/// • Waits <see cref="SupplierTenderSyncSettings.DelayBetweenRequestsSeconds"/> between pages.
/// • Retries each page up to <see cref="SupplierTenderSyncSettings.PageRetryCount"/> times
///   with exponential back-off (Polly).
/// • Continues to the next page if a single page fails (per-page try/catch).
/// • Stops early after <see cref="SupplierTenderSyncSettings.MaxConsecutiveEmptyPages"/>
///   consecutive empty pages.
/// • Calls SaveChangesAsync once per page (EF Core bulk save pattern).
/// </summary>
public class SupplierTenderSyncService : ISupplierTenderSyncService
{
    // Reusable JSON options – camelCase is the default Etimad response casing.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TenderDbContext _db;
    private readonly SupplierTenderSyncSettings _settings;
    private readonly ILogger<SupplierTenderSyncService> _logger;

    public SupplierTenderSyncService(
        IHttpClientFactory httpClientFactory,
        TenderDbContext db,
        SupplierTenderSyncSettings settings,
        ILogger<SupplierTenderSyncService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _db                = db;
        _settings          = settings;
        _logger            = logger;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<SupplierTenderSyncResult> SyncAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new SupplierTenderSyncResult { StartedAt = DateTime.UtcNow };

        _logger.LogInformation(
            "=== SupplierTenderSync started. BaseUrl={Url}, PageSize={Size}, PublishDateId={DateId} ===",
            _settings.BaseUrl, _settings.PageSize, _settings.PublishDateId);

        // Build a Polly retry pipeline for transient HTTP failures.
        var retryPipeline = BuildRetryPipeline();

        int pageNumber        = 1;
        int totalPages        = 1; // will be calculated from the first successful response
        int consecutiveEmpty  = 0;

        try
        {
            while (pageNumber <= totalPages && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "--- Fetching page {Page}/{Total} ---", pageNumber, totalPages);

                // Enforce a minimum 5-second delay between requests (skip before page 1).
                if (pageNumber > 1)
                    await Task.Delay(
                        TimeSpan.FromSeconds(40),
                        cancellationToken);

                // Per-page try/catch: a single failing page must not abort the whole sync.
                try
                {
                    var response = await retryPipeline.ExecuteAsync(
                        async ct => await FetchPageAsync(pageNumber, ct),
                        cancellationToken);

                    // Calculate total pages from the first valid response.
                    if (pageNumber == 1 && response != null && response.TotalCount > 0)
                    {
                        totalPages = (int)Math.Ceiling(
                            (double)response.TotalCount / _settings.PageSize);

                        _logger.LogInformation(
                            "Total tenders={Total}, PageSize={Size} → TotalPages={Pages}",
                            response.TotalCount, _settings.PageSize, totalPages);
                    }

                    var items = response?.Data;

                    // Handle empty / null data from the API.
                    if (items == null || items.Count == 0)
                    {
                        _logger.LogWarning(
                            "Page {Page} returned no data (empty). ConsecutiveEmpty={Count}",
                            pageNumber, consecutiveEmpty + 1);

                        consecutiveEmpty++;

                        if (consecutiveEmpty >= _settings.MaxConsecutiveEmptyPages)
                        {
                            _logger.LogWarning(
                                "Reached {Max} consecutive empty pages – stopping sync.",
                                _settings.MaxConsecutiveEmptyPages);
                            break;
                        }

                        result.TotalPagesProcessed++;
                        pageNumber++;
                        continue;
                    }

                    _logger.LogInformation(
                        "Page {Page}: received {Count} tenders.", pageNumber, items.Count);

                    // Reset empty-page counter now that we have data.
                    consecutiveEmpty = 0;
                    result.TotalFetched += items.Count;

                    // Upsert the current page into SQL Server.
                    var (inserted, updated, failed) =
                        await UpsertPageAsync(items, cancellationToken);

                    result.TotalInserted += inserted;
                    result.TotalUpdated  += updated;
                    result.TotalFailed   += failed;

                    _logger.LogInformation(
                        "Page {Page} saved — inserted={I}, updated={U}, failed={F}",
                        pageNumber, inserted, updated, failed);
                }
                catch (OperationCanceledException)
                {
                    // Propagate cancellation – do not swallow it.
                    throw;
                }
                catch (Exception ex)
                {
                    // Record the error but continue with the next page.
                    result.TotalFailedPages++;
                    var msg = $"Page {pageNumber} failed: {ex.Message}";
                    result.Errors.Add(msg);
                    _logger.LogError(ex, "Error processing page {Page}. Continuing…", pageNumber);
                }

                result.TotalPagesProcessed++;
                pageNumber++;
            }

            result.Success = result.TotalFailedPages == 0;
            result.Message = result.Success
                ? $"Sync completed. Pages={result.TotalPagesProcessed}, " +
                  $"Fetched={result.TotalFetched}, Inserted={result.TotalInserted}, " +
                  $"Updated={result.TotalUpdated}."
                : $"Sync completed with {result.TotalFailedPages} failed page(s). " +
                  $"Pages={result.TotalPagesProcessed}, Inserted={result.TotalInserted}, " +
                  $"Updated={result.TotalUpdated}.";
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Message = "Sync was cancelled by the caller.";
            _logger.LogWarning("SupplierTenderSync was cancelled.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync failed with unexpected error: {ex.Message}";
            result.Errors.Add(ex.ToString());
            _logger.LogCritical(ex, "Unexpected fatal error in SupplierTenderSync.");
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "=== SupplierTenderSync finished. Success={Success}, Duration={Duration}, " +
                "Pages={Pages}, Fetched={Fetched}, Inserted={Inserted}, Updated={Updated}, " +
                "Failed={Failed}, FailedPages={FailedPages} ===",
                result.Success, result.Duration, result.TotalPagesProcessed,
                result.TotalFetched, result.TotalInserted, result.TotalUpdated,
                result.TotalFailed, result.TotalFailedPages);
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Calls the Etimad JSON endpoint for a single page and deserialises the response.
    /// Throws on non-success HTTP status so Polly can retry.
    /// </summary>
    private async Task<SupplierTenderPageResponse?> FetchPageAsync(
        int pageNumber, CancellationToken cancellationToken)
    {
        // Build the full request URL with query parameters.
        var url = $"{_settings.BaseUrl.TrimEnd('/')}?" +
                  $"PageSize={_settings.PageSize}" +
                  $"&PublishDateId={_settings.PublishDateId}" +
                  $"&pageNumber={pageNumber}";

        _logger.LogDebug("GET {Url}", url);

        var client = _httpClientFactory.CreateClient("EtimadClient");
        var httpResponse = await client.GetAsync(url, cancellationToken);

        // Throw so Polly can decide whether to retry.
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        // The Etimad platform sits behind an F5 BIG-IP WAF.
        // When the WAF detects automated traffic it returns a full HTML
        // bot-challenge page (200 OK) instead of JSON.
        // Detect this early so the error message is actionable.
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('{'))
        {
            throw new InvalidOperationException(
                "Anti-bot: the server returned an HTML challenge page instead of JSON. " +
                "The WAF has flagged this request as automated traffic. " +
                $"Response preview: {json[..Math.Min(json.Length, 200)]}");
        }

        return JsonSerializer.Deserialize<SupplierTenderPageResponse>(json, _jsonOptions);
    }

    /// <summary>
    /// Upserts a list of API tender items into the SupplierTenders table.
    /// Loads existing rows by <c>TenderId</c> in a single query, then
    /// adds new entities or updates existing ones before calling
    /// <see cref="DbContext.SaveChangesAsync"/>.
    /// </summary>
    /// <returns>Tuple of (inserted, updated, failed) counts.</returns>
    private async Task<(int Inserted, int Updated, int Failed)> UpsertPageAsync(
        List<SupplierTenderItemDto> items,
        CancellationToken cancellationToken)
    {
        int inserted = 0, updated = 0, failed = 0;

        // Collect all tender IDs on this page for a single DB lookup.
        var incomingIds = items
            .Where(i => i.TenderId > 0)
            .Select(i => i.TenderId)
            .Distinct()
            .ToList();

        // Load only the rows we might update – avoids a full table scan.
        var existingMap = await _db.SupplierTenders
            .Where(e => incomingIds.Contains(e.TenderId))
            .ToDictionaryAsync(e => e.TenderId, cancellationToken);

        foreach (var item in items)
        {
            // Basic guard: skip records with no meaningful ID.
            if (item.TenderId <= 0)
            {
                _logger.LogWarning(
                    "Skipping tender with TenderId=0 (ReferenceNumber={Ref})",
                    item.ReferenceNumber ?? "N/A");
                failed++;
                continue;
            }

            try
            {
                if (existingMap.TryGetValue(item.TenderId, out var existing))
                {
                    // Update the existing row with the latest data from the API.
                    ApplyUpdate(existing, item);
                    updated++;
                }
                else
                {
                    // Insert a new row.
                    var entity = MapToEntity(item);
                    _db.SupplierTenders.Add(entity);
                    inserted++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "Failed to prepare upsert for TenderId={Id}", item.TenderId);
            }
        }

        // Persist the entire page in one round-trip (EF Core bulk save pattern).
        await _db.SaveChangesAsync(cancellationToken);

        return (inserted, updated, failed);
    }

    /// <summary>Maps a DTO to a new <see cref="SupplierTenderEntity"/>.</summary>
    private static SupplierTenderEntity MapToEntity(SupplierTenderItemDto dto) => new()
    {
        TenderId                  = dto.TenderId,
        ReferenceNumber           = dto.ReferenceNumber,
        TenderName                = dto.TenderName,
        TenderNumber              = dto.TenderNumber,
        BranchName                = dto.BranchName,
        AgencyName                = dto.AgencyName,
        TenderIdString            = dto.TenderIdString,
        TenderStatusId            = dto.TenderStatusId,
        TenderTypeId              = dto.TenderTypeId,
        TenderTypeName            = dto.TenderTypeName,
        LastEnqueriesDate         = dto.LastEnqueriesDate,
        LastOfferPresentationDate = dto.LastOfferPresentationDate,
        OffersOpeningDate         = dto.OffersOpeningDate,
        TenderActivityId          = dto.TenderActivityId,
        SubmitionDate             = dto.SubmitionDate,
        FinancialFees             = dto.FinancialFees,
        InvitationCost            = dto.InvitationCost,
        BuyingCost                = dto.BuyingCost,
        RemainingDays             = dto.RemainingDays,
        RemainingHours            = dto.RemainingHours,
        RemainingMins             = dto.RemainingMins,
        CurrentDateTime           = dto.CurrentDateTime,
        FirstSyncedAt             = DateTime.UtcNow,
        LastSyncedAt              = DateTime.UtcNow
    };

    /// <summary>Copies the latest API data onto an existing <see cref="SupplierTenderEntity"/>.</summary>
    private static void ApplyUpdate(SupplierTenderEntity entity, SupplierTenderItemDto dto)
    {
        entity.ReferenceNumber           = dto.ReferenceNumber;
        entity.TenderName                = dto.TenderName;
        entity.TenderNumber              = dto.TenderNumber;
        entity.BranchName                = dto.BranchName;
        entity.AgencyName                = dto.AgencyName;
        entity.TenderIdString            = dto.TenderIdString;
        entity.TenderStatusId            = dto.TenderStatusId;
        entity.TenderTypeId              = dto.TenderTypeId;
        entity.TenderTypeName            = dto.TenderTypeName;
        entity.LastEnqueriesDate         = dto.LastEnqueriesDate;
        entity.LastOfferPresentationDate = dto.LastOfferPresentationDate;
        entity.OffersOpeningDate         = dto.OffersOpeningDate;
        entity.TenderActivityId          = dto.TenderActivityId;
        entity.SubmitionDate             = dto.SubmitionDate;
        entity.FinancialFees             = dto.FinancialFees;
        entity.InvitationCost            = dto.InvitationCost;
        entity.BuyingCost                = dto.BuyingCost;
        entity.RemainingDays             = dto.RemainingDays;
        entity.RemainingHours            = dto.RemainingHours;
        entity.RemainingMins             = dto.RemainingMins;
        entity.CurrentDateTime           = dto.CurrentDateTime;
        entity.LastSyncedAt              = DateTime.UtcNow;
    }

    /// <summary>
    /// Builds a Polly retry pipeline for transient HTTP and I/O errors.
    /// Uses exponential back-off with jitter.
    /// CAPTCHA / anti-bot responses are not retried.
    /// </summary>
    private ResiliencePipeline<SupplierTenderPageResponse?> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<SupplierTenderPageResponse?>()
            .AddRetry(new RetryStrategyOptions<SupplierTenderPageResponse?>
            {
                MaxRetryAttempts = _settings.PageRetryCount,
                Delay            = TimeSpan.FromMilliseconds(_settings.RetryBaseDelayMs),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle     = new PredicateBuilder<SupplierTenderPageResponse?>()
                    .Handle<Exception>(ex =>
                        // Only retry transient failures; abort on cancellation or hard blocks.
                        ex is not OperationCanceledException &&
                        !ex.Message.Contains("Anti-bot", StringComparison.OrdinalIgnoreCase) &&
                        !ex.Message.Contains("CAPTCHA",  StringComparison.OrdinalIgnoreCase)),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "HTTP retry {Attempt}/{Max} for supplier tenders page, delay={Delay}ms",
                        args.AttemptNumber + 1,
                        _settings.PageRetryCount,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
