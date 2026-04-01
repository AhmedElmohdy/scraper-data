using System.Text.Json.Serialization;

namespace EtimadScraper.Models;

// ---------------------------------------------------------------------------
// Root envelope returned by:
//   GET /Tender/AllSupplierTendersForVisitorAsync?PageSize=6&PublishDateId=5&pageNumber=N
// ---------------------------------------------------------------------------

/// <summary>
/// Root envelope returned by the Etimad supplier-tenders JSON endpoint.
/// </summary>
public class SupplierTenderPageResponse
{
    /// <summary>Total number of tenders available across all pages.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>Number of items in each page as returned by the API.</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>Current page number (1-based).</summary>
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    /// <summary>Tender records for this page. May be null when the page is empty.</summary>
    [JsonPropertyName("data")]
    public List<SupplierTenderItemDto>? Data { get; set; }
}

/// <summary>
/// A single tender record from the Etimad supplier-tenders API.
/// All fields mirror the JSON keys returned by the endpoint.
/// </summary>
public class SupplierTenderItemDto
{
    [JsonPropertyName("tenderId")]
    public int TenderId { get; set; }

    /// <summary>Human-readable reference number (e.g. "2025/ITC-4371").</summary>
    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("tenderName")]
    public string? TenderName { get; set; }

    [JsonPropertyName("tenderNumber")]
    public string? TenderNumber { get; set; }

    /// <summary>Branch or region name.</summary>
    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    /// <summary>Issuing government agency.</summary>
    [JsonPropertyName("agencyName")]
    public string? AgencyName { get; set; }

    /// <summary>String representation of the tender ID (may be a formatted code).</summary>
    [JsonPropertyName("tenderIdString")]
    public string? TenderIdString { get; set; }

    [JsonPropertyName("tenderStatusId")]
    public int? TenderStatusId { get; set; }

    [JsonPropertyName("tenderTypeId")]
    public int? TenderTypeId { get; set; }

    [JsonPropertyName("tenderTypeName")]
    public string? TenderTypeName { get; set; }

    /// <summary>Deadline for submitting enquiries.</summary>
    [JsonPropertyName("lastEnqueriesDate")]
    public string? LastEnqueriesDate { get; set; }

    /// <summary>Deadline for submitting the offer.</summary>
    [JsonPropertyName("lastOfferPresentationDate")]
    public string? LastOfferPresentationDate { get; set; }

    /// <summary>Date when offers will be publicly opened.</summary>
    [JsonPropertyName("offersOpeningDate")]
    public string? OffersOpeningDate { get; set; }

    [JsonPropertyName("tenderActivityId")]
    public int? TenderActivityId { get; set; }

    /// <summary>Date when the tender was published / submitted.</summary>
    [JsonPropertyName("submitionDate")]
    public string? SubmitionDate { get; set; }

    [JsonPropertyName("financialFees")]
    public decimal? FinancialFees { get; set; }

    [JsonPropertyName("invitationCost")]
    public decimal? InvitationCost { get; set; }

    [JsonPropertyName("buyingCost")]
    public decimal? BuyingCost { get; set; }

    /// <summary>Remaining days until offer deadline.</summary>
    [JsonPropertyName("remainingDays")]
    public int? RemainingDays { get; set; }

    [JsonPropertyName("remainingHours")]
    public int? RemainingHours { get; set; }

    [JsonPropertyName("remainingMins")]
    public int? RemainingMins { get; set; }

    /// <summary>Server timestamp at the moment the page was generated.</summary>
    [JsonPropertyName("currentDateTime")]
    public string? CurrentDateTime { get; set; }
}
