using EtimadScraper.Configuration;

namespace EtimadScraper.Services;

/// <summary>
/// Thread-safe singleton store for the tender evaluation background job state.
///
/// Thread-safety strategy:
/// • <c>_totalRuns</c> is incremented with <see cref="Interlocked.Increment"/>.
/// • All other mutable fields are written inside a <c>lock</c> so a reader
///   always sees a consistent snapshot.
/// • <c>IsEnabled</c> is derived from whether an API key is configured.
/// </summary>
public sealed class TenderEvaluationJobState : ITenderEvaluationJobState
{
    private readonly TenderEvaluationSettings _settings;
    private readonly object _lock = new();

    private bool _isRunning;
    private DateTime? _lastStartedAt;
    private DateTime? _lastCompletedAt;
    private DateTime? _lastSuccessAt;
    private string? _lastError;
    private string? _lastMessage;
    private int _totalRuns;

    private int _lastTotalCandidates;
    private int _lastTotalEvaluated;
    private int _lastTotalFailed;

    public TenderEvaluationJobState(TenderEvaluationSettings settings)
    {
        _settings = settings;
    }

    // ?? Readers ??????????????????????????????????????????????????????????????

    public bool IsEnabled => _settings.Enabled && !string.IsNullOrWhiteSpace(_settings.OpenRouterApiKey);
    public bool IsRunning { get { lock (_lock) return _isRunning; } }
    public DateTime? LastStartedAt { get { lock (_lock) return _lastStartedAt; } }
    public DateTime? LastCompletedAt { get { lock (_lock) return _lastCompletedAt; } }
    public DateTime? LastSuccessAt { get { lock (_lock) return _lastSuccessAt; } }
    public string? LastError { get { lock (_lock) return _lastError; } }
    public string? LastMessage { get { lock (_lock) return _lastMessage; } }
    public int TotalRuns => Volatile.Read(ref _totalRuns);

    public int LastTotalCandidates { get { lock (_lock) return _lastTotalCandidates; } }
    public int LastTotalEvaluated { get { lock (_lock) return _lastTotalEvaluated; } }
    public int LastTotalFailed { get { lock (_lock) return _lastTotalFailed; } }

    // ?? Writers ???????????????????????????????????????????????????????????????

    public void MarkStarted()
    {
        Interlocked.Increment(ref _totalRuns);

        lock (_lock)
        {
            _isRunning            = true;
            _lastStartedAt        = DateTime.UtcNow;
            _lastError            = null;
            _lastTotalCandidates  = 0;
            _lastTotalEvaluated   = 0;
            _lastTotalFailed      = 0;
        }
    }

    public void MarkSucceeded(string message, int candidates, int evaluated, int failed)
    {
        lock (_lock)
        {
            _isRunning           = false;
            _lastCompletedAt     = DateTime.UtcNow;
            _lastSuccessAt       = _lastCompletedAt;
            _lastMessage         = message;
            _lastError           = null;
            _lastTotalCandidates = candidates;
            _lastTotalEvaluated  = evaluated;
            _lastTotalFailed     = failed;
        }
    }

    public void MarkFailed(string error, string message, int candidates, int evaluated, int failed)
    {
        lock (_lock)
        {
            _isRunning           = false;
            _lastCompletedAt     = DateTime.UtcNow;
            _lastError           = error;
            _lastMessage         = message;
            _lastTotalCandidates = candidates;
            _lastTotalEvaluated  = evaluated;
            _lastTotalFailed     = failed;
        }
    }
}
