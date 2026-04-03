using EtimadScraper.Models;

namespace EtimadScraper.Services;

/// <summary>
/// Contract for the AI-based tender evaluation service.
/// Evaluates unevaluated <c>SupplierTenders</c> records using the OpenRouter API
/// and persists the <c>MatchingScore</c>, <c>MatchingReason</c>, and
/// <c>Evaluated</c> fields back to the database.
/// </summary>
public interface ITenderEvaluationService
{
    /// <summary>
    /// Evaluates all <c>SupplierTenders</c> rows where <c>Evaluated != true</c>,
    /// sending them to the AI in configurable chunks.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the evaluation run.</param>
    Task<TenderEvaluationResult> EvaluateUnevaluatedTendersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a single ad-hoc tender (by name and agency) to the AI and returns
    /// the evaluation result without persisting anything to the database.
    /// </summary>
    /// <param name="tenderName">The tender name to evaluate.</param>
    /// <param name="agencyName">The agency name to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    Task<TenderEvaluationTestResponse> TestEvaluateAsync(
        string tenderName,
        string agencyName,
        CancellationToken cancellationToken = default);
}
