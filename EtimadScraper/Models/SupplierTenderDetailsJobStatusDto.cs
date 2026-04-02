namespace EtimadScraper.Models;

/// <summary>
/// Snapshot of the supplier-tender <em>details</em> background job's runtime state.
/// Returned by <c>GET /api/supplier-tender-details-sync/status</c>.
/// </summary>
public sealed class SupplierTenderDetailsJobStatusDto
{
    /// <summary>Whether the job is enabled in configuration.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>True while a sync execution is actively running.</summary>
    public bool IsRunning { get; init; }

    /// <summary>UTC timestamp when the most recent run started. Null if never run.</summary>
    public DateTime? LastRunTime { get; init; }

    /// <summary>UTC timestamp when the most recent run completed (success or failure). Null if never completed.</summary>
    public DateTime? LastCompletedAt { get; init; }

    /// <summary>UTC timestamp of the last run that completed successfully. Null if never succeeded.</summary>
    public DateTime? LastSuccessTime { get; init; }

    /// <summary>Human-readable summary from the last completed run.</summary>
    public string? LastResultMessage { get; init; }

    /// <summary>Error message from the last failed run. Null if the last run succeeded.</summary>
    public string? LastError { get; init; }

    /// <summary>Total execution cycles since the host launched.</summary>
    public int TotalRuns { get; init; }

    // Last-run counters
    public int TotalProcessed { get; init; }
    public int TotalInserted  { get; init; }
    public int TotalSkipped   { get; init; }
    public int TotalFailed    { get; init; }
}
