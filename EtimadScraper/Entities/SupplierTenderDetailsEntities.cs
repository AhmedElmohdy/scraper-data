namespace EtimadScraper.Entities;

/// <summary>
/// Stores the <c>BasicInformation</c> section of a scraped tender.
/// Linked to <see cref="SupplierTenderEntity"/> via <see cref="TenderId"/>.
/// Table: SupplierTendersDetialsMain
/// </summary>
public class SupplierTendersDetialsMain
{
    public int Id { get; set; }

    /// <summary>Etimad tender ID (string form, e.g. encrypted ID used in URLs).</summary>
    public string TenderId { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? TenderNumberIAM { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Purpose { get; set; }
    public string? DocumentsValue { get; set; }
    public string? Status { get; set; }
    public string? ContractDuration { get; set; }
    public string? MaintenanceInsurance { get; set; }
    public string? CompetitionType { get; set; }
    public string? Organization { get; set; }
    public string? RemainingTime { get; set; }
    public string? SubmissionMethod { get; set; }
    public string? InitialGuaranteeRequirements { get; set; }
    public string? InitialGuaranteeTitle { get; set; }
    public string? InitialGuaranteeValue { get; set; }
    public string? FinalGuarantee { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stores the <c>DatesAndDeadlines</c> section of a scraped tender.
/// Table: SupplierTendersDetialsDates
/// </summary>
public class SupplierTendersDetialsDates
{
    public int Id { get; set; }
    public string TenderId { get; set; } = string.Empty;

    public string? InquiryDeadline { get; set; }
    public string? SubmissionDeadline { get; set; }
    public string? OfferOpeningDate { get; set; }
    public string? TechnicalOfferOpeningDate { get; set; }
    public string? StopPeriod { get; set; }
    public string? ExpectedAwardDate { get; set; }
    public string? ActionStartDate { get; set; }
    public string? QuestionSubmissionStartDate { get; set; }
    public string? MaxQuestionResponseTime { get; set; }
    public string? OpeningPlace { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stores the <c>ClassificationAndExecution</c> section of a scraped tender.
/// Table: SupplierTendersDetialsRelations
/// </summary>
public class SupplierTendersDetialsRelations
{
    public int Id { get; set; }
    public string TenderId { get; set; } = string.Empty;

    public string? TenderCondition { get; set; }
    public string? ExecutionLocation { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? SupplyItemsIncluded { get; set; }
    public string? ConstructionWorks { get; set; }
    public string? MaintenanceAndOperationWorks { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stores the <c>AwardingResults</c> section of a scraped tender.
/// Table: SupplierTendersDetialsAwarding
/// </summary>
public class SupplierTendersDetialsAwarding
{
    public int Id { get; set; }
    public string TenderId { get; set; } = string.Empty;

    public string? AwardingResultStatus { get; set; }
    public string? AwardingResultMessage { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stores the <c>LocalContent</c> section of a scraped tender.
/// Table: SupplierTendersDetialsLocalContent
/// </summary>
public class SupplierTendersDetialsLocalContent
{
    public int Id { get; set; }
    public string TenderId { get; set; } = string.Empty;

    public string? LocalContentRequirements { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}
