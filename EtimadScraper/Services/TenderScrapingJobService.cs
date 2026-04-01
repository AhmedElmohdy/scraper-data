using EtimadScraper.Configuration;
using EtimadScraper.Models;
using Polly;
using Polly.Retry;

namespace EtimadScraper.Services;

/// <summary>
/// Orchestrates the full end-to-end scraping run:
///   1. Start from page 1.
///   2. Scrape page-by-page until no data is returned (or the safety cap is hit).
///   3. Persist each page's tenders to SQL Server via <see cref="TenderPersistenceService"/>.
///   4. Stop on consecutive empty pages or unrecoverable errors.
/// </summary>
public class TenderScrapingJobService
{
    private readonly EtimadScraperService _scraperService;
    private readonly TenderPersistenceService _persistenceService;
    private readonly ScrapingJobSettings _jobSettings;
    private readonly ILogger<TenderScrapingJobService> _logger;

    // Polly retry pipeline – rebuilt once per instance (thread-safe).
    private readonly ResiliencePipeline<List<TenderDto>> _retryPipeline;

    public TenderScrapingJobService(
        EtimadScraperService scraperService,
        TenderPersistenceService persistenceService,
        ScrapingJobSettings jobSettings,
        ILogger<TenderScrapingJobService> logger)
    {
        _scraperService    = scraperService;
        _persistenceService = persistenceService;
        _jobSettings        = jobSettings;
        _logger             = logger;

        _retryPipeline = BuildRetryPipeline();
    }

    /// <summary>
    /// Runs the full scraping job from page 1 until exhausted.
    /// Safe for repeated execution (idempotent upsert).
    /// </summary>
    public async Task<ScrapingJobResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var result = new ScrapingJobResult { StartedAt = DateTime.UtcNow };

        _logger.LogInformation("=== Scraping job started at {StartedAt} ===", result.StartedAt);

        int consecutiveEmpty = 0;
        int pageNumber       = 1;


        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Safety cap
                if (_jobSettings.MaxPagesToScrape > 0 && pageNumber > _jobSettings.MaxPagesToScrape)
                {
                    _logger.LogInformation("Reached configured page cap ({Cap}). Stopping.", _jobSettings.MaxPagesToScrape);
                    break;
                }

                _logger.LogInformation("--- Scraping page {Page} ---", pageNumber);

                List<TenderDto>? pageTenders;

                try
                {
                    pageTenders = await _retryPipeline.ExecuteAsync(
                        async ct => await _scraperService.ScrapeTendersAsync(pageNumber, pageNumber, ct),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Job was cancelled while scraping page {Page}.", pageNumber);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unrecoverable error on page {Page}. Stopping job.", pageNumber);
                    result.Errors.Add($"Page {pageNumber}: {ex.Message}");
                    break;
                }

                // ?? Empty page detection ?????????????????????????????????????
                if (pageTenders is null || pageTenders.Count == 0)
                {
                    consecutiveEmpty++;
                    _logger.LogWarning("Page {Page} returned no tenders ({Count}/{Max} consecutive empty pages).",
                        pageNumber, consecutiveEmpty, _jobSettings.MaxConsecutiveEmptyPages);

                    if (consecutiveEmpty >= _jobSettings.MaxConsecutiveEmptyPages)
                    {
                        _logger.LogInformation("Consecutive empty page limit reached. No more data available.");
                        break;
                    }

                    pageNumber++;
                    await DelayAsync(cancellationToken);
                    continue;
                }

                int newInPage = pageTenders.Count;
                int skippedInPage = 0;

                result.TotalScraped        += pageTenders.Count;
                result.TotalSkipped        += skippedInPage;
                result.TotalPagesProcessed++;

                _logger.LogInformation(
                    "Page {Page}: scraped={Scraped}, new={New}, skipped(dup)={Skipped}",
                    pageNumber, pageTenders.Count, newInPage, skippedInPage);

                // Persist current page immediately and report insert/update result.
                var (ins, upd, fail) = await _persistenceService.UpsertBatchAsync(pageTenders, cancellationToken);
                result.TotalInserted += ins;
                result.TotalUpdated  += upd;
                result.TotalFailed   += fail;

                _logger.LogInformation(
                    "Page {Page} saved: inserted={Inserted}, updated={Updated}, failed={Failed}",
                    pageNumber, ins, upd, fail);

                consecutiveEmpty = 0;

                pageNumber++;
                await DelayAsync(cancellationToken);
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success
                ? $"Job completed successfully. Pages: {result.TotalPagesProcessed}, " +
                  $"Scraped: {result.TotalScraped}, Inserted: {result.TotalInserted}, " +
                  $"Updated: {result.TotalUpdated}, Failed: {result.TotalFailed}."
                : $"Job completed with {result.Errors.Count} error(s). " +
                  $"Pages: {result.TotalPagesProcessed}, Inserted: {result.TotalInserted}, " +
                  $"Updated: {result.TotalUpdated}.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Job failed with unexpected error: {ex.Message}";
            result.Errors.Add(ex.ToString());
            _logger.LogCritical(ex, "Unexpected fatal error in scraping job.");
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "=== Scraping job finished. Success={Success}, Duration={Duration}, " +
                "Pages={Pages}, Scraped={Scraped}, Inserted={Inserted}, Updated={Updated}, " +
                "Failed={Failed}, Skipped={Skipped} ===",
                result.Success, result.Duration, result.TotalPagesProcessed,
                result.TotalScraped, result.TotalInserted, result.TotalUpdated,
                result.TotalFailed, result.TotalSkipped);
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task DelayAsync(CancellationToken cancellationToken)
    {
        if (_jobSettings.DelayBetweenPagesMs > 0)
            await Task.Delay(_jobSettings.DelayBetweenPagesMs, cancellationToken);
    }

    private ResiliencePipeline<List<TenderDto>> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<List<TenderDto>>()
            .AddRetry(new RetryStrategyOptions<List<TenderDto>>
            {
                MaxRetryAttempts = _jobSettings.PageRetryCount,
                Delay            = TimeSpan.FromMilliseconds(_jobSettings.RetryBaseDelayMs),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle     = new PredicateBuilder<List<TenderDto>>()
                    .Handle<Exception>(ex =>
                        // Only retry transient/recoverable exceptions; skip anti-bot hard stops.
                        ex is not OperationCanceledException &&
                        !ex.Message.Contains("Anti-bot") &&
                        !ex.Message.Contains("CAPTCHA")),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry {Attempt}/{Max} after {Delay}ms",
                        args.AttemptNumber + 1,
                        _jobSettings.PageRetryCount,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
