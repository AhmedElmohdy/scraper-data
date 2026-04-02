namespace EtimadScraper.Models;

/// <summary>
/// Summary returned by <c>POST /api/supplier-tenders/sync-details</c>.
/// </summary>
public class SupplierTenderDetailsSyncResult
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;

    public int TotalTendersScanned { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalInserted { get; set; }
    public int TotalFailed { get; set; }

    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Per-tender error messages collected during the run.</summary>
    public List<string> Errors { get; set; } = [];
}
