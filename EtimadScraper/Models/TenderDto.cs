namespace EtimadScraper.Models;

/// <summary>
/// Data Transfer Object representing a tender from the Etimad platform
/// </summary>
public class TenderDto
{
    /// <summary>
    /// Unique tender reference number
    /// </summary>
    public string TenderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tender title or description
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Organization or entity issuing the tender
    /// </summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Date when the tender was published
    /// </summary>
    public string PublishDate { get; set; } = string.Empty;

    /// <summary>
    /// Deadline date for submitting bids
    /// </summary>
    public string ClosingDate { get; set; } = string.Empty;

    /// <summary>
    /// Full URL to the tender details page
    /// </summary>
    public string DetailsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Category or department information (highlighted section)
    /// Example: "?????? ???? ????? ??????? - ????? ?????????"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Department or sub-category badge shown in blue
    /// Example: "?????? ???? ????? ??????? - ????? ?????????" or "???? ??????? ?????????? - ??????? ??????? - ?????????"
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Status of the tender (e.g., Open, Closed, Awarded)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Additional information or tender type
    /// </summary>
    public string AdditionalInfo { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this record was scraped
    /// </summary>
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{TenderNumber} - {Title} ({Organization})";
    }
}
