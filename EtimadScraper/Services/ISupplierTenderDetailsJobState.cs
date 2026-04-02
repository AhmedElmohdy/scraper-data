namespace EtimadScraper.Services;

/// <summary>
/// Tracks the live runtime state of <c>SupplierTenderDetailsSyncBackgroundJob</c>.
/// Registered as a singleton so both the background job (writer) and the
/// status controller (reader) share the same instance.
/// </summary>
public interface ISupplierTenderDetailsJobState
{
    bool      IsEnabled       { get; }
    bool      IsRunning       { get; }
    DateTime? LastStartedAt   { get; }
    DateTime? LastCompletedAt { get; }
    DateTime? LastSuccessAt   { get; }
    string?   LastError       { get; }
    string?   LastMessage     { get; }
    int       TotalRuns       { get; }

    // Last-run counters
    int LastTotalProcessed { get; }
    int LastTotalInserted  { get; }
    int LastTotalSkipped   { get; }
    int LastTotalFailed    { get; }

    /// <summary>Called at the beginning of every execution cycle.</summary>
    void MarkStarted();

    /// <summary>Called when a run finishes successfully.</summary>
    void MarkSucceeded(string message, int processed, int inserted, int skipped, int failed);

    /// <summary>Called when a run finishes with an error.</summary>
    void MarkFailed(string error, string message, int processed, int inserted, int skipped, int failed);
}
