namespace EtimadScraper.Models;

/// <summary>
/// Snapshot of the supplier-tender background job's runtime state.
/// Returned by GET /api/job-status/supplier-tender.
/// </summary>
public sealed class SupplierTenderJobStatusDto
{
    /// <summary>Whether the job is enabled in configuration.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>True while a sync execution is actively running.</summary>
    public bool IsRunning { get; init; }

    /// <summary>UTC timestamp when the most recent run started. Null if never run.</summary>
    public DateTime? LastStartedAt { get; init; }

    /// <summary>UTC timestamp when the most recent run finished (success or failure). Null if never completed.</summary>
    public DateTime? LastCompletedAt { get; init; }

    /// <summary>UTC timestamp of the last run that completed successfully. Null if never succeeded.</summary>
    public DateTime? LastSuccessAt { get; init; }

    /// <summary>Error message from the last failed run. Null if the last run succeeded.</summary>
    public string? LastError { get; init; }

    /// <summary>Human-readable summary message from the last completed run.</summary>
    public string? LastMessage { get; init; }

    /// <summary>Total number of execution cycles started since the host launched.</summary>
    public int TotalRuns { get; init; }
}
