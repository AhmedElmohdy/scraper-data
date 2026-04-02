using EtimadScraper.Configuration;

namespace EtimadScraper.Services;

/// <summary>
/// Thread-safe singleton store for the supplier-tender <em>details</em> background job state.
///
/// Thread-safety strategy:
/// • <c>_totalRuns</c> is incremented with <see cref="Interlocked.Increment"/>.
/// • All other mutable fields are written inside a <c>lock</c> so a reader
///   always sees a consistent snapshot.
/// • <c>IsEnabled</c> is read directly from the injected settings singleton.
/// </summary>
public sealed class SupplierTenderDetailsJobState : ISupplierTenderDetailsJobState
{
    private readonly SupplierTenderDetailsSyncSettings _settings;
    private readonly object _lock = new();

    private bool      _isRunning;
    private DateTime? _lastStartedAt;
    private DateTime? _lastCompletedAt;
    private DateTime? _lastSuccessAt;
    private string?   _lastError;
    private string?   _lastMessage;
    private int       _totalRuns;

    private int _lastTotalProcessed;
    private int _lastTotalInserted;
    private int _lastTotalSkipped;
    private int _lastTotalFailed;

    public SupplierTenderDetailsJobState(SupplierTenderDetailsSyncSettings settings)
    {
        _settings = settings;
    }

    // ?? Readers ??????????????????????????????????????????????????????????????

    public bool      IsEnabled       => _settings.Enabled;
    public bool      IsRunning       { get { lock (_lock) return _isRunning;         } }
    public DateTime? LastStartedAt   { get { lock (_lock) return _lastStartedAt;     } }
    public DateTime? LastCompletedAt { get { lock (_lock) return _lastCompletedAt;   } }
    public DateTime? LastSuccessAt   { get { lock (_lock) return _lastSuccessAt;     } }
    public string?   LastError       { get { lock (_lock) return _lastError;         } }
    public string?   LastMessage     { get { lock (_lock) return _lastMessage;       } }
    public int       TotalRuns       => Volatile.Read(ref _totalRuns);

    public int LastTotalProcessed { get { lock (_lock) return _lastTotalProcessed; } }
    public int LastTotalInserted  { get { lock (_lock) return _lastTotalInserted;  } }
    public int LastTotalSkipped   { get { lock (_lock) return _lastTotalSkipped;   } }
    public int LastTotalFailed    { get { lock (_lock) return _lastTotalFailed;    } }

    // ?? Writers ???????????????????????????????????????????????????????????????

    public void MarkStarted()
    {
        Interlocked.Increment(ref _totalRuns);

        lock (_lock)
        {
            _isRunning          = true;
            _lastStartedAt      = DateTime.UtcNow;
            _lastError          = null;
            _lastTotalProcessed = 0;
            _lastTotalInserted  = 0;
            _lastTotalSkipped   = 0;
            _lastTotalFailed    = 0;
        }
    }

    public void MarkSucceeded(string message, int processed, int inserted, int skipped, int failed)
    {
        lock (_lock)
        {
            _isRunning          = false;
            _lastCompletedAt    = DateTime.UtcNow;
            _lastSuccessAt      = _lastCompletedAt;
            _lastMessage        = message;
            _lastError          = null;
            _lastTotalProcessed = processed;
            _lastTotalInserted  = inserted;
            _lastTotalSkipped   = skipped;
            _lastTotalFailed    = failed;
        }
    }

    public void MarkFailed(string error, string message, int processed, int inserted, int skipped, int failed)
    {
        lock (_lock)
        {
            _isRunning          = false;
            _lastCompletedAt    = DateTime.UtcNow;
            _lastError          = error;
            _lastMessage        = message;
            _lastTotalProcessed = processed;
            _lastTotalInserted  = inserted;
            _lastTotalSkipped   = skipped;
            _lastTotalFailed    = failed;
        }
    }
}
