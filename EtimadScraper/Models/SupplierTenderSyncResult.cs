namespace EtimadScraper.Models;

/// <summary>
/// Statistics summary for supplier tenders, grouped by InOutStatus and date.
/// </summary>
public class TenderStatisticsDto
{
    // ?? Total Tenders card ?????????????????????????????????????????????
    public int TotalTenders       { get; set; }
    public int TotalTodayTenders  { get; set; }

    // ?? Matching card ??????????????????????????????????????????????????
    public int MatchingTotal   { get; set; }
    public int MatchingIn      { get; set; }
    public int MatchingOut     { get; set; }
    public int MatchingPending { get; set; }

    // ?? Total IN card ?????????????????????????????????????????????????
    public int TotalIn        { get; set; }
    public int TotalInToday   { get; set; }

    // ?? Evaluated card ????????????????????????????????????????????????
    public int EvaluatedTotal { get; set; }
    public int EvaluatedToday { get; set; }
}

/// <summary>
/// Request body for updating the InOutStatus of a supplier tender.
/// </summary>
public class UpdateInOutStatusRequest
{
    /// <summary>New workflow status value (e.g. "pending", "reviewed", "approved").</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Summary result returned after a supplier-tender sync operation.
/// </summary>
public class SupplierTenderSyncResult
{
    /// <summary>Whether the sync completed without fatal errors.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable summary message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Total pages processed (including empty pages).</summary>
    public int TotalPagesProcessed { get; set; }

    /// <summary>Total tender records returned by the API across all pages.</summary>
    public int TotalFetched { get; set; }

    /// <summary>Records successfully inserted (new tenders).</summary>
    public int TotalInserted { get; set; }

    /// <summary>Records successfully updated (existing tenders with fresh data).</summary>
    public int TotalUpdated { get; set; }

    /// <summary>Records that failed validation or DB persistence.</summary>
    public int TotalFailed { get; set; }

    /// <summary>Number of pages that returned an HTTP/parse error (process continued).</summary>
    public int TotalFailedPages { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>Elapsed duration (null while still running).</summary>
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    /// <summary>Per-page error messages collected during the sync.</summary>
    public List<string> Errors { get; set; } = new();
}
