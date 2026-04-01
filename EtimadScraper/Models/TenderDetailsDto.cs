namespace EtimadScraper.Models;

public class TenderDetailsDto
{
    public string TenderId { get; set; } = string.Empty;

    public BasicInformationSection BasicInformation { get; set; } = new();
    public DatesAndDeadlinesSection DatesAndDeadlines { get; set; } = new();
    public ClassificationAndExecutionSection ClassificationAndExecution { get; set; } = new();
    public AwardingResultsSection AwardingResults { get; set; } = new();
    public LocalContentSection LocalContent { get; set; } = new();
    public DebugSection Debug { get; set; } = new();
    public MetadataSection Metadata { get; set; } = new();

    public override string ToString() =>
        $"{BasicInformation.ReferenceNumber} - {BasicInformation.Title}";
}

public class BasicInformationSection
{
    public string Title { get; set; } = string.Empty;
    public string TenderNumberIAM { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string DocumentsValue { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ContractDuration { get; set; } = string.Empty;
    public string MaintenanceInsurance { get; set; } = string.Empty;
    public string CompetitionType { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string RemainingTime { get; set; } = string.Empty;
    public string SubmissionMethod { get; set; } = string.Empty;
    public string InitialGuaranteeRequirements { get; set; } = string.Empty;
    public string InitialGuaranteeTitle { get; set; } = string.Empty;
    public string InitialGuaranteeValue { get; set; } = string.Empty;
    public string FinalGuarantee { get; set; } = string.Empty;
}

public class DatesAndDeadlinesSection
{
    public string InquiryDeadline { get; set; } = string.Empty;
    public string SubmissionDeadline { get; set; } = string.Empty;
    public string OfferOpeningDate { get; set; } = string.Empty;
    public string TechnicalOfferOpeningDate { get; set; } = string.Empty;
    public string StopPeriod { get; set; } = string.Empty;
    public string ExpectedAwardDate { get; set; } = string.Empty;
    public string ActionStartDate { get; set; } = string.Empty;
    public string QuestionSubmissionStartDate { get; set; } = string.Empty;
    public string MaxQuestionResponseTime { get; set; } = string.Empty;
    public string OpeningPlace { get; set; } = string.Empty;
}

public class ClassificationAndExecutionSection
{
    public string TenderCondition { get; set; } = string.Empty;
    public string ExecutionLocation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SupplyItemsIncluded { get; set; } = string.Empty;
    public string ConstructionWorks { get; set; } = string.Empty;
    public string MaintenanceAndOperationWorks { get; set; } = string.Empty;
}

public class AwardingResultsSection
{
    public string AwardingResultStatus { get; set; } = string.Empty;
    public string AwardingResultMessage { get; set; } = string.Empty;
}

public class LocalContentSection
{
    public string LocalContentRequirements { get; set; } = string.Empty;
}

public class DebugSection
{
    public Dictionary<string, string> AllFields { get; set; } = new();
    public string AdditionalInfo { get; set; } = string.Empty;
    public string? RawHtml { get; set; }
}

public class MetadataSection
{
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public string DataSource { get; set; } = "Scraped";
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
