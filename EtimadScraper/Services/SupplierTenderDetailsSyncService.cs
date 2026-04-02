using EtimadScraper.Data;
using EtimadScraper.Entities;
using EtimadScraper.Models;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper.Services;

/// <summary>
/// Iterates every row in <c>SupplierTenders</c>, skips those whose details
/// already exist in <c>SupplierTendersDetialsMain</c>, and for all others:
/// <list type="number">
///   <item>Calls <see cref="TenderDetailsScraperService.ScrapeTenderDetailsAsync"/>.</item>
///   <item>Persists all five detail sections in a single transaction.</item>
///   <item>Waits 30 seconds before processing the next tender.</item>
/// </list>
/// </summary>
public class SupplierTenderDetailsSyncService : ISupplierTenderDetailsSyncService
{
    private static readonly TimeSpan DelayBetweenTenders = TimeSpan.FromSeconds(30);

    private readonly IDbContextFactory<TenderDbContext> _dbFactory;
    private readonly TenderDetailsScraperService _scraper;
    private readonly ILogger<SupplierTenderDetailsSyncService> _logger;

    public SupplierTenderDetailsSyncService(
        IDbContextFactory<TenderDbContext> dbFactory,
        TenderDetailsScraperService scraper,
        ILogger<SupplierTenderDetailsSyncService> logger)
    {
        _dbFactory = dbFactory;
        _scraper   = scraper;
        _logger    = logger;
    }

    /// <inheritdoc/>
    public async Task<SupplierTenderDetailsSyncResult> SyncDetailsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new SupplierTenderDetailsSyncResult { StartedAt = DateTime.UtcNow };

        _logger.LogInformation("=== SupplierTenderDetailsSync started ===");

        try
        {
            // ------------------------------------------------------------------
            // 1. Load all SupplierTender IDs in one round-trip.
            // ------------------------------------------------------------------
            List<string> tenderIds;
            using (var db = _dbFactory.CreateDbContext())
            {
                tenderIds = await db.SupplierTenders
                    .Where(t => t.TenderIdString != null && t.TenderIdString != string.Empty)
                    .Select(t => t.TenderIdString!)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            result.TotalTendersScanned = tenderIds.Count;

            _logger.LogInformation(
                "Found {Count} unique TenderIds to process.", tenderIds.Count);

            // ------------------------------------------------------------------
            // 2. Process each tender one by one.
            // ------------------------------------------------------------------
            for (int i = 0; i < tenderIds.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tenderId = tenderIds[i];

                if (string.IsNullOrWhiteSpace(tenderId))
                {
                    _logger.LogWarning(
                        "[{Index}/{Total}] Skipping empty TenderId.", i + 1, tenderIds.Count);
                    result.TotalFailed++;
                    continue;
                }

                try
                {
                    // -------------------------------------------------------
                    // 3. Existence check — use the Main table as the sentinel.
                    // -------------------------------------------------------
                    using var db = _dbFactory.CreateDbContext();

                    bool alreadyExists = await db.SupplierTendersDetialsMain
                        .AnyAsync(m => m.TenderId == tenderId, cancellationToken);

                    if (alreadyExists)
                    {
                        _logger.LogInformation(
                            "[{Index}/{Total}] TenderId={Id} — SKIPPED (details already exist).",
                            i + 1, tenderIds.Count, tenderId);
                        result.TotalSkipped++;
                    }
                    else
                    {
                        // -------------------------------------------------------
                        // 4. Scrape the five endpoints.
                        // -------------------------------------------------------
                        _logger.LogInformation(
                            "[{Index}/{Total}] TenderId={Id} — scraping started.",
                            i + 1, tenderIds.Count, tenderId);

                        var dto = await _scraper.ScrapeTenderDetailsAsync(tenderId);

                        if (!dto.Metadata.IsSuccess)
                        {
                            var errMsg =
                                $"TenderId={tenderId}: scraping failed — {dto.Metadata.ErrorMessage}";
                            _logger.LogError(
                                "[{Index}/{Total}] {Message}",
                                i + 1, tenderIds.Count, errMsg);
                            result.TotalFailed++;
                            result.Errors.Add(errMsg);
                        }
                        else
                        {
                            // -------------------------------------------------------
                            // 5. Persist all five sections in a single transaction.
                            // -------------------------------------------------------
                            await SaveDetailsSectionsAsync(db, tenderId, dto, cancellationToken);

                            _logger.LogInformation(
                                "[{Index}/{Total}] TenderId={Id} — scraping SUCCESS, details saved.",
                                i + 1, tenderIds.Count, tenderId);
                            result.TotalInserted++;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var errMsg = $"TenderId={tenderId}: unexpected error — {ex.Message}";
                    _logger.LogError(ex,
                        "[{Index}/{Total}] {Message}",
                        i + 1, tenderIds.Count, errMsg);
                    result.TotalFailed++;
                    result.Errors.Add(errMsg);
                }

                // ---------------------------------------------------------------
                // 6. Wait 30 seconds before the next iteration (skip after last).
                // ---------------------------------------------------------------
                if (i < tenderIds.Count - 1)
                {
                    _logger.LogInformation(
                        "[{Index}/{Total}] Waiting {Delay}s before next tender…",
                        i + 1, tenderIds.Count, DelayBetweenTenders.TotalSeconds);

                    await Task.Delay(DelayBetweenTenders, cancellationToken);
                }
            }

            result.Success = result.TotalFailed == 0;
            result.Message = result.Success
                ? $"Sync complete. Scanned={result.TotalTendersScanned}, " +
                  $"Inserted={result.TotalInserted}, Skipped={result.TotalSkipped}."
                : $"Sync complete with {result.TotalFailed} failure(s). " +
                  $"Scanned={result.TotalTendersScanned}, Inserted={result.TotalInserted}, " +
                  $"Skipped={result.TotalSkipped}, Failed={result.TotalFailed}.";
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Message = "Sync was cancelled.";
            _logger.LogWarning("SupplierTenderDetailsSync was cancelled.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync failed: {ex.Message}";
            result.Errors.Add(ex.ToString());
            _logger.LogCritical(ex, "Fatal error in SupplierTenderDetailsSync.");
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "=== SupplierTenderDetailsSync finished. Duration={Duration}, " +
                "Scanned={Scanned}, Inserted={Inserted}, Skipped={Skipped}, Failed={Failed} ===",
                result.Duration, result.TotalTendersScanned,
                result.TotalInserted, result.TotalSkipped, result.TotalFailed);
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Saves all five detail sections for a single tender within one EF Core
    /// transaction so either all sections are written or none are.
    /// </summary>
    private static async Task SaveDetailsSectionsAsync(
        TenderDbContext db,
        string tenderId,
        TenderDetailsDto dto,
        CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        db.SupplierTendersDetialsMain.Add(MapMain(tenderId, dto));
        db.SupplierTendersDetialsDates.Add(MapDates(tenderId, dto));
        db.SupplierTendersDetialsRelations.Add(MapRelations(tenderId, dto));
        db.SupplierTendersDetialsAwarding.Add(MapAwarding(tenderId, dto));
        db.SupplierTendersDetialsLocalContent.Add(MapLocalContent(tenderId, dto));

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static SupplierTendersDetialsMain MapMain(string tenderId, TenderDetailsDto dto) => new()
    {
        TenderId                      = tenderId,
        Title                         = dto.BasicInformation.Title,
        TenderNumberIAM               = dto.BasicInformation.TenderNumberIAM,
        ReferenceNumber               = dto.BasicInformation.ReferenceNumber,
        Purpose                       = dto.BasicInformation.Purpose,
        DocumentsValue                = dto.BasicInformation.DocumentsValue,
        Status                        = dto.BasicInformation.Status,
        ContractDuration              = dto.BasicInformation.ContractDuration,
        MaintenanceInsurance          = dto.BasicInformation.MaintenanceInsurance,
        CompetitionType               = dto.BasicInformation.CompetitionType,
        Organization                  = dto.BasicInformation.Organization,
        RemainingTime                 = dto.BasicInformation.RemainingTime,
        SubmissionMethod              = dto.BasicInformation.SubmissionMethod,
        InitialGuaranteeRequirements  = dto.BasicInformation.InitialGuaranteeRequirements,
        InitialGuaranteeTitle         = dto.BasicInformation.InitialGuaranteeTitle,
        InitialGuaranteeValue         = dto.BasicInformation.InitialGuaranteeValue,
        FinalGuarantee                = dto.BasicInformation.FinalGuarantee,
        ScrapedAt                     = dto.Metadata.ScrapedAt
    };

    private static SupplierTendersDetialsDates MapDates(string tenderId, TenderDetailsDto dto) => new()
    {
        TenderId                      = tenderId,
        InquiryDeadline               = dto.DatesAndDeadlines.InquiryDeadline,
        SubmissionDeadline            = dto.DatesAndDeadlines.SubmissionDeadline,
        OfferOpeningDate              = dto.DatesAndDeadlines.OfferOpeningDate,
        TechnicalOfferOpeningDate     = dto.DatesAndDeadlines.TechnicalOfferOpeningDate,
        StopPeriod                    = dto.DatesAndDeadlines.StopPeriod,
        ExpectedAwardDate             = dto.DatesAndDeadlines.ExpectedAwardDate,
        ActionStartDate               = dto.DatesAndDeadlines.ActionStartDate,
        QuestionSubmissionStartDate   = dto.DatesAndDeadlines.QuestionSubmissionStartDate,
        MaxQuestionResponseTime       = dto.DatesAndDeadlines.MaxQuestionResponseTime,
        OpeningPlace                  = dto.DatesAndDeadlines.OpeningPlace,
        ScrapedAt                     = dto.Metadata.ScrapedAt
    };

    private static SupplierTendersDetialsRelations MapRelations(string tenderId, TenderDetailsDto dto) => new()
    {
        TenderId                      = tenderId,
        TenderCondition               = dto.ClassificationAndExecution.TenderCondition,
        ExecutionLocation             = dto.ClassificationAndExecution.ExecutionLocation,
        Description                   = dto.ClassificationAndExecution.Description,
        Category                      = dto.ClassificationAndExecution.Category,
        SupplyItemsIncluded           = dto.ClassificationAndExecution.SupplyItemsIncluded,
        ConstructionWorks             = dto.ClassificationAndExecution.ConstructionWorks,
        MaintenanceAndOperationWorks  = dto.ClassificationAndExecution.MaintenanceAndOperationWorks,
        ScrapedAt                     = dto.Metadata.ScrapedAt
    };

    private static SupplierTendersDetialsAwarding MapAwarding(string tenderId, TenderDetailsDto dto) => new()
    {
        TenderId               = tenderId,
        AwardingResultStatus   = dto.AwardingResults.AwardingResultStatus,
        AwardingResultMessage  = dto.AwardingResults.AwardingResultMessage,
        ScrapedAt              = dto.Metadata.ScrapedAt
    };

    private static SupplierTendersDetialsLocalContent MapLocalContent(string tenderId, TenderDetailsDto dto) => new()
    {
        TenderId                    = tenderId,
        LocalContentRequirements    = dto.LocalContent.LocalContentRequirements,
        ScrapedAt                   = dto.Metadata.ScrapedAt
    };
}
