using EtimadScraper.Data;
using EtimadScraper.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper.Controllers;

/// <summary>
/// Exposes read-only endpoints for querying stored supplier-tender detail sections.
///
/// GET /api/supplier-tenders/details/{tenderId}
/// </summary>
[ApiController]
[Route("api/supplier-tenders-integration")]
public class SupplierTenderDetailsController : ControllerBase
{
    private readonly TenderDbContext _db;
    private readonly ILogger<SupplierTenderDetailsController> _logger;

    public SupplierTenderDetailsController(
        TenderDbContext db,
        ILogger<SupplierTenderDetailsController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns all five detail sections (Main, Dates, Relations, Awarding,
    /// LocalContent) for the given <paramref name="tenderId"/>.
    /// </summary>
    /// <param name="tenderId">The string tender ID stored in the detail tables.</param>
    /// <param name="cancellationToken">Standard ASP.NET Core cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="SupplierTenderDetailsResponse"/> when at least one
    /// section is found, or 404 Not Found when none of the five tables contain a
    /// row for the requested <paramref name="tenderId"/>.
    /// </returns>
    [HttpGet("details/{tenderId}")]
    [ProducesResponseType(typeof(SupplierTenderDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetailsByTenderId(
        string tenderId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GET /api/supplier-tenders/details/{TenderId} — querying detail tables.",
            tenderId);

        var main = await _db.SupplierTendersDetialsMain
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenderId == tenderId, cancellationToken);

        var dates = await _db.SupplierTendersDetialsDates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenderId == tenderId, cancellationToken);

        var relations = await _db.SupplierTendersDetialsRelations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenderId == tenderId, cancellationToken);

        var awarding = await _db.SupplierTendersDetialsAwarding
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenderId == tenderId, cancellationToken);

        var localContent = await _db.SupplierTendersDetialsLocalContent
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenderId == tenderId, cancellationToken);

        if (main is null && dates is null && relations is null
            && awarding is null && localContent is null)
        {
            _logger.LogWarning(
                "No detail records found for TenderId={TenderId}.", tenderId);
            return NotFound(new { message = $"No details found for TenderId '{tenderId}'." });
        }

        var response = new SupplierTenderDetailsResponse
        {
            TenderId = tenderId,

            Main = main is null ? null : new SupplierTenderMainDto
            {
                Id                           = main.Id,
                Title                        = main.Title,
                TenderNumberIAM              = main.TenderNumberIAM,
                ReferenceNumber              = main.ReferenceNumber,
                Purpose                      = main.Purpose,
                DocumentsValue               = main.DocumentsValue,
                Status                       = main.Status,
                ContractDuration             = main.ContractDuration,
                MaintenanceInsurance         = main.MaintenanceInsurance,
                CompetitionType              = main.CompetitionType,
                Organization                 = main.Organization,
                RemainingTime                = main.RemainingTime,
                SubmissionMethod             = main.SubmissionMethod,
                InitialGuaranteeRequirements = main.InitialGuaranteeRequirements,
                InitialGuaranteeTitle        = main.InitialGuaranteeTitle,
                InitialGuaranteeValue        = main.InitialGuaranteeValue,
                FinalGuarantee               = main.FinalGuarantee,
                ScrapedAt                    = main.ScrapedAt,
            },

            Dates = dates is null ? null : new SupplierTenderDatesDto
            {
                Id                          = dates.Id,
                InquiryDeadline             = dates.InquiryDeadline,
                SubmissionDeadline          = dates.SubmissionDeadline,
                OfferOpeningDate            = dates.OfferOpeningDate,
                TechnicalOfferOpeningDate   = dates.TechnicalOfferOpeningDate,
                StopPeriod                  = dates.StopPeriod,
                ExpectedAwardDate           = dates.ExpectedAwardDate,
                ActionStartDate             = dates.ActionStartDate,
                QuestionSubmissionStartDate = dates.QuestionSubmissionStartDate,
                MaxQuestionResponseTime     = dates.MaxQuestionResponseTime,
                OpeningPlace                = dates.OpeningPlace,
                ScrapedAt                   = dates.ScrapedAt,
            },

            Relations = relations is null ? null : new SupplierTenderRelationsDto
            {
                Id                           = relations.Id,
                TenderCondition              = relations.TenderCondition,
                ExecutionLocation            = relations.ExecutionLocation,
                Description                  = relations.Description,
                Category                     = relations.Category,
                SupplyItemsIncluded          = relations.SupplyItemsIncluded,
                ConstructionWorks            = relations.ConstructionWorks,
                MaintenanceAndOperationWorks = relations.MaintenanceAndOperationWorks,
                ScrapedAt                    = relations.ScrapedAt,
            },

            Awarding = awarding is null ? null : new SupplierTenderAwardingDto
            {
                Id                    = awarding.Id,
                AwardingResultStatus  = awarding.AwardingResultStatus,
                AwardingResultMessage = awarding.AwardingResultMessage,
                ScrapedAt             = awarding.ScrapedAt,
            },

            LocalContent = localContent is null ? null : new SupplierTenderLocalContentDto
            {
                Id                       = localContent.Id,
                LocalContentRequirements = localContent.LocalContentRequirements,
                ScrapedAt                = localContent.ScrapedAt,
            },
        };

        return Ok(response);
    }
}
