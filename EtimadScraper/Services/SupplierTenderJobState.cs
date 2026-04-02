using EtimadScraper.Configuration;

namespace EtimadScraper.Services;

/// <summary>
/// Thread-safe, singleton store for the supplier-tender background job state.
///
/// Thread-safety strategy:
/// • <c>_totalRuns</c> is incremented with <see cref="Interlocked.Increment"/>.
/// • All other mutable fields are written inside a <c>lock</c> so that a
///   reader taking a snapshot always sees a consistent set of values.
/// • <c>IsEnabled</c> is read directly from the injected settings object,
///   which is itself a singleton — no extra locking required.
/// </summary>
public sealed class SupplierTenderJobState : ISupplierTenderJobState
{
    private readonly SupplierTenderSyncSettings _settings;

    // Lock object for all non-atomic state fields.
    private readonly object _lock = new();

    private bool      _isRunning;
    private DateTime? _lastStartedAt;
    private DateTime? _lastCompletedAt;
    private DateTime? _lastSuccessAt;
    private string?   _lastError;
    private string?   _lastMessage;
    private int       _totalRuns;

    public SupplierTenderJobState(SupplierTenderSyncSettings settings)
    {
        _settings = settings;
    }

    // ?? Readers (no lock needed for bool/DateTime?/string? on 64-bit; lock for consistency) ??

    public bool      IsEnabled       => _settings.Enabled;
    public bool      IsRunning       { get { lock (_lock) return _isRunning;       } }
    public DateTime? LastStartedAt   { get { lock (_lock) return _lastStartedAt;   } }
    public DateTime? LastCompletedAt { get { lock (_lock) return _lastCompletedAt; } }
    public DateTime? LastSuccessAt   { get { lock (_lock) return _lastSuccessAt;   } }
    public string?   LastError       { get { lock (_lock) return _lastError;       } }
    public string?   LastMessage     { get { lock (_lock) return _lastMessage;     } }
    public int       TotalRuns       => Volatile.Read(ref _totalRuns);

    // ?? Writers ?????????????????????????????????????????????????????????????

    public void MarkStarted()
    {
        Interlocked.Increment(ref _totalRuns);

        lock (_lock)
        {
            _isRunning     = true;
            _lastStartedAt = DateTime.UtcNow;
            _lastError     = null; // clear previous error on new attempt
        }
    }

    public void MarkSucceeded(string message)
    {
        lock (_lock)
        {
            _isRunning       = false;
            _lastCompletedAt = DateTime.UtcNow;
            _lastSuccessAt   = _lastCompletedAt;
            _lastMessage     = message;
            _lastError       = null;
        }
    }

    public void MarkFailed(string error, string message)
    {
        lock (_lock)
        {
            _isRunning       = false;
            _lastCompletedAt = DateTime.UtcNow;
            _lastError       = error;
            _lastMessage     = message;
        }
    }
}
