namespace EtimadScraper.Models;

/// <summary>Summary returned by a full-scrape job run.</summary>
public class ScrapingJobResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public int TotalPagesProcessed { get; set; }
    public int TotalScraped { get; set; }
    public int TotalInserted { get; set; }
    public int TotalUpdated { get; set; }
    public int TotalFailed { get; set; }
    public int TotalSkipped { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    public List<string> Errors { get; set; } = new();
}
