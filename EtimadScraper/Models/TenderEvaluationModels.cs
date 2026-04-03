namespace EtimadScraper.Models;

/// <summary>
/// Summary result returned after one AI evaluation run.
/// </summary>
public class TenderEvaluationResult
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration => (CompletedAt ?? DateTime.UtcNow) - StartedAt;

    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Total unevaluated tenders found at the start of the run.</summary>
    public int TotalCandidates { get; set; }

    /// <summary>Tenders successfully evaluated and persisted.</summary>
    public int TotalEvaluated { get; set; }

    /// <summary>Tenders that could not be updated (parse error, DB error, etc.).</summary>
    public int TotalFailed { get; set; }

    /// <summary>Number of AI chunks processed.</summary>
    public int ChunksProcessed { get; set; }

    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Result of a single ad-hoc AI evaluation test (test endpoint).
/// </summary>
public class TenderEvaluationTestResponse
{
    public string? TenderName { get; set; }
    public string? AgencyName { get; set; }
    public decimal? MatchingScore { get; set; }
    public string? MatchingReason { get; set; }
    public bool? Evaluated { get; set; }
}

/// <summary>
/// Current runtime status of the tender evaluation background job.
/// </summary>
public class TenderEvaluationJobStatusDto
{
    public bool IsEnabled { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastRunTime { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public string? LastResultMessage { get; set; }
    public string? LastError { get; set; }
    public int TotalRuns { get; set; }
    public int LastTotalCandidates { get; set; }
    public int LastTotalEvaluated { get; set; }
    public int LastTotalFailed { get; set; }
}
