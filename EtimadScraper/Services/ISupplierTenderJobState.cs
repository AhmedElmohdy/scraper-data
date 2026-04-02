namespace EtimadScraper.Services;

/// <summary>
/// Tracks the live runtime state of <c>SupplierTenderSyncBackgroundJob</c>.
/// Registered as a singleton so both the background job (writer) and the
/// status controller (reader) share the same instance.
/// </summary>
public interface ISupplierTenderJobState
{
    bool      IsEnabled      { get; }
    bool      IsRunning      { get; }
    DateTime? LastStartedAt  { get; }
    DateTime? LastCompletedAt { get; }
    DateTime? LastSuccessAt  { get; }
    string?   LastError      { get; }
    string?   LastMessage    { get; }
    int       TotalRuns      { get; }

    /// <summary>Called at the beginning of every execution cycle.</summary>
    void MarkStarted();

    /// <summary>Called when a run finishes successfully.</summary>
    void MarkSucceeded(string message);

    /// <summary>Called when a run finishes with an error.</summary>
    void MarkFailed(string error, string message);
}
