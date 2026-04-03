using EtimadScraper.Configuration;
using EtimadScraper.Data;
using EtimadScraper.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtimadScraper.Services;

/// <summary>
/// Evaluates unevaluated <c>SupplierTenders</c> rows using the OpenRouter chat
/// completions API and persists the AI score and reason back to SQL Server.
///
/// Key behaviours:
/// • Loads all rows where <c>Evaluated != true</c> in one query.
/// • Builds a minimal JSON list (id, name, agency) and sends it to the AI in
///   configurable chunks (default: 50 items).
/// • Waits <see cref="TenderEvaluationSettings.DelayBetweenChunksMs"/> ms between chunks.
/// • Safely parses plain JSON arrays, markdown-fenced JSON, or object wrappers
///   (keys <c>tenders</c> / <c>evaluations</c> / <c>results</c>).
/// • Updates each matched entity with <c>MatchingScore</c>, <c>MatchingReason</c>,
///   and <c>Evaluated = true</c>, then calls SaveChangesAsync once per chunk.
/// • All scores are stored; callers can filter by
///   <c>MatchingScore >= MinMatchingScore</c> independently.
/// </summary>
public class TenderEvaluationService : ITenderEvaluationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1/chat/completions";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TenderDbContext _db;
    private readonly TenderEvaluationSettings _settings;
    private readonly ILogger<TenderEvaluationService> _logger;

    public TenderEvaluationService(
        IHttpClientFactory httpClientFactory,
        TenderDbContext db,
        TenderEvaluationSettings settings,
        ILogger<TenderEvaluationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _db                = db;
        _settings          = settings;
        _logger            = logger;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<TenderEvaluationTestResponse> TestEvaluateAsync(
        string tenderName,
        string agencyName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "TenderEvaluation test: TenderName={Name}, AgencyName={Agency}",
            tenderName, agencyName);

        var aiList = new[]
        {
            new { id = "test-1", name = tenderName, agency = agencyName }
        };

        var aiResults = await CallOpenRouterAsync(aiList, cancellationToken);

        var item = aiResults?.FirstOrDefault();

        return new TenderEvaluationTestResponse
        {
            TenderName    = tenderName,
            AgencyName    = agencyName,
            MatchingScore  = item?.MatchingScore,
            MatchingReason = item?.MatchingReason,
            Evaluated      = item != null
        };
    }

    /// <inheritdoc/>
    public async Task<TenderEvaluationResult> EvaluateUnevaluatedTendersAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new TenderEvaluationResult { StartedAt = DateTime.UtcNow };

        _logger.LogInformation(
            "=== TenderEvaluation started. Model={Model}, ChunkSize={Chunk}, MinScore={Min} ===",
            _settings.ModelName, _settings.ChunkSize, _settings.MinMatchingScore);

        try
        {
            // Load all unevaluated tenders (tracked so we can update them).
            var candidates = await _db.SupplierTenders
                .Where(t => t.Evaluated != true)
                .ToListAsync(cancellationToken);

            //var date = new DateTime(2026, 3, 29, 23, 29, 33, 396);

            //var candidates = await _db.SupplierTenders
            //    .Where(t => t.Evaluated != true
            //             && t.SubmitionDate > date)
            //    .ToListAsync(cancellationToken);

            result.TotalCandidates = candidates.Count;

            if (candidates.Count == 0)
            {
                result.Success = true;
                result.Message = "No unevaluated tenders found.";
                _logger.LogInformation("TenderEvaluation: no unevaluated tenders – nothing to do.");
                return result;
            }

            _logger.LogInformation(
                "TenderEvaluation: found {Count} unevaluated tenders. " +
                "Processing in chunks of {ChunkSize}.",
                candidates.Count, _settings.ChunkSize);

            // Build a lookup by the identifier that will be sent to the AI.
            // We use ReferenceNumber when available, otherwise the entity Id as a string.
            var entityById = candidates.ToDictionary(
                t => string.IsNullOrWhiteSpace(t.ReferenceNumber)
                    ? t.Id.ToString()
                    : t.ReferenceNumber,
                t => t);

            // Split into chunks and evaluate.
            var chunks = candidates
                .Chunk(_settings.ChunkSize)
                .ToList();

            for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Delay between chunks (skip before the very first one).
                if (chunkIndex > 0)
                    await Task.Delay(_settings.DelayBetweenChunksMs, cancellationToken);

                var chunk = chunks[chunkIndex];

                _logger.LogInformation(
                    "TenderEvaluation: processing chunk {Idx}/{Total} ({Count} tenders).",
                    chunkIndex + 1, chunks.Count, chunk.Length);

                try
                {
                    // Build the simplified list that the AI will receive.
                    var aiList = chunk.Select(t => new
                    {
                        id     = string.IsNullOrWhiteSpace(t.ReferenceNumber)
                                     ? t.Id.ToString()
                                     : t.ReferenceNumber,
                        name   = t.TenderName ?? string.Empty,
                        agency = t.AgencyName ?? string.Empty
                    }).ToList();

                    var aiResults = await CallOpenRouterAsync(aiList, cancellationToken);

                    if (aiResults == null || aiResults.Count == 0)
                    {
                        _logger.LogWarning(
                            "TenderEvaluation: chunk {Idx} returned no AI results – skipping.",
                            chunkIndex + 1);
                        result.TotalFailed += chunk.Length;
                        continue;
                    }

                    // Persist the AI scores for this chunk.
                    int chunkEvaluated = 0;
                    int chunkFailed    = 0;

                    foreach (var aiItem in aiResults)
                    {
                        if (string.IsNullOrWhiteSpace(aiItem.Id))
                            continue;

                        if (!entityById.TryGetValue(aiItem.Id, out var entity))
                        {
                            _logger.LogDebug(
                                "TenderEvaluation: AI returned unknown id={Id} – ignoring.",
                                aiItem.Id);
                            continue;
                        }

                        entity.MatchingScore  = aiItem.MatchingScore;
                        entity.MatchingReason = aiItem.MatchingReason;
                        entity.Evaluated      = true;
                        entity.LastSyncedAt   = DateTime.UtcNow;
                        chunkEvaluated++;
                    }

                    // Mark any entities in the chunk that the AI did not return at all
                    // as evaluated (to avoid re-processing on every run).
                    foreach (var t in chunk)
                    {
                        var key = string.IsNullOrWhiteSpace(t.ReferenceNumber)
                            ? t.Id.ToString()
                            : t.ReferenceNumber;

                        if (t.Evaluated != true)
                        {
                            // AI did not return a result for this tender.
                            t.Evaluated    = true;
                            t.MatchingScore  = 0;
                            t.MatchingReason = "Not evaluated by AI in this run.";
                            t.LastSyncedAt   = DateTime.UtcNow;
                            chunkFailed++;
                        }
                    }

                    await _db.SaveChangesAsync(cancellationToken);

                    result.TotalEvaluated  += chunkEvaluated;
                    result.TotalFailed     += chunkFailed;
                    result.ChunksProcessed++;

                    _logger.LogInformation(
                        "TenderEvaluation: chunk {Idx} saved — evaluated={E}, missed={M}.",
                        chunkIndex + 1, chunkEvaluated, chunkFailed);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.TotalFailed += chunk.Length;
                    var msg = $"Chunk {chunkIndex + 1} failed: {ex.Message}";
                    result.Errors.Add(msg);
                    _logger.LogError(ex,
                        "TenderEvaluation: error processing chunk {Idx}. Continuing…",
                        chunkIndex + 1);
                }
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success
                ? $"Evaluation completed. Candidates={result.TotalCandidates}, " +
                  $"Evaluated={result.TotalEvaluated}, Chunks={result.ChunksProcessed}."
                : $"Evaluation completed with errors. Candidates={result.TotalCandidates}, " +
                  $"Evaluated={result.TotalEvaluated}, Failed={result.TotalFailed}.";
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Message = "Evaluation was cancelled by the caller.";
            _logger.LogWarning("TenderEvaluation was cancelled.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Evaluation failed with unexpected error: {ex.Message}";
            result.Errors.Add(ex.ToString());
            _logger.LogCritical(ex, "Unexpected fatal error in TenderEvaluation.");
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "=== TenderEvaluation finished. Success={Success}, Duration={Duration}, " +
                "Candidates={Candidates}, Evaluated={Evaluated}, Failed={Failed}, " +
                "Chunks={Chunks} ===",
                result.Success, result.Duration, result.TotalCandidates,
                result.TotalEvaluated, result.TotalFailed, result.ChunksProcessed);
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sends one chunk of tender summaries to the OpenRouter chat completions API
    /// and returns the parsed list of AI evaluation results.
    /// </summary>
    private async Task<List<AiEvaluationItem>?> CallOpenRouterAsync(
        object aiList,
        CancellationToken cancellationToken)
    {
        var tendersJson = JsonSerializer.Serialize(aiList);

        var fullPrompt =
            _settings.MatchingScorePrompt +
            "\n\nHere is the list of tenders (JSON):\n" +
            tendersJson +
            "\n\nReturn ONLY a valid JSON array (no explanation, no markdown) where each element has: " +
            "\"id\" (string), \"matchingScore\" (number 0-100), \"matchingReason\" (string).";

        var requestBody = new
        {
            model = _settings.ModelName,
            messages = new object[]
            {
            new
            {
                role = "system",
                content = @"You are a strict and professional business development manager.

You evaluate tenders based on how well they match the company core services.

Key rules:
- Software development and mobile/smart device applications are considered a STRONG DIRECT MATCH.
- Do NOT require GIS or remote sensing to give a high score.
- Give high scores (80-95) for software, platforms, mobile apps, or digital solutions.
- Give very high scores (90-100) if multiple domains match (AI, GIS, data platforms).
- Government and public sector entities can increase relevance.
- Be realistic and consistent in scoring.
- Return ONLY valid JSON."
            },
            new
            {
                role = "user",
                content = fullPrompt
            }
            },
            max_tokens = _settings.MaxTokens,
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(requestBody);

        var client = _httpClientFactory.CreateClient("OpenRouterClient");

        using var request = new HttpRequestMessage(HttpMethod.Post, OpenRouterBaseUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_settings.OpenRouterApiKey}");
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://your-app-url.com");
        request.Headers.TryAddWithoutValidation("X-Title", "Tender Evaluation");

        _logger.LogDebug("TenderEvaluation: calling OpenRouter model={Model}.", _settings.ModelName);

        using var httpResponse = await client.SendAsync(request, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            _logger.LogWarning("TenderEvaluation: OpenRouter returned empty response.");
            return null;
        }

        return ParseAiResponse(responseJson);
    }
    /// <summary>
    /// Extracts the AI's text content from the OpenRouter response envelope
    /// and then parses it as a list of <see cref="AiEvaluationItem"/>.
    ///
    /// Handles:
    /// • Plain JSON array   ? <c>[{ ... }, ...]</c>
    /// • Markdown-fenced    ? <c>```json\n[...]\n```</c>
    /// • Object wrapper     ? <c>{ "tenders": [...] }</c> or <c>{ "evaluations": [...] }</c>
    /// </summary>
    private List<AiEvaluationItem>? ParseAiResponse(string responseJson)
    {
        try
        {
            // 1. Extract the text content from the OpenRouter chat completion envelope.
            using var doc = JsonDocument.Parse(responseJson);

            string? content = null;

            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    content = contentProp.GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning(
                    "TenderEvaluation: OpenRouter response contained no usable content.");
                return null;
            }

            // 2. Strip markdown fences if present.
            content = content.Trim();
            if (content.StartsWith("```"))
            {
                var firstNewline = content.IndexOf('\n');
                var lastFence    = content.LastIndexOf("```");

                if (firstNewline >= 0 && lastFence > firstNewline)
                    content = content[(firstNewline + 1)..lastFence].Trim();
            }

            // 3. Try plain JSON array first.
            if (content.StartsWith("["))
                return JsonSerializer.Deserialize<List<AiEvaluationItem>>(content, _jsonOptions);

            // 4. Try object wrapper with common array key names.
            if (content.StartsWith("{"))
            {
                using var innerDoc = JsonDocument.Parse(content);
                var root            = innerDoc.RootElement;

                foreach (var key in new[] { "tenders", "evaluations", "results", "data" })
                {
                    if (root.TryGetProperty(key, out var arrayProp) &&
                        arrayProp.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<AiEvaluationItem>>(
                            arrayProp.GetRawText(), _jsonOptions);
                    }
                }
            }

            _logger.LogWarning(
                "TenderEvaluation: could not interpret AI content as a JSON array. " +
                "Preview: {Preview}",
                content[..Math.Min(content.Length, 300)]);

            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "TenderEvaluation: JSON parse error in AI response.");
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Inner DTO – used only for deserialising the AI response
    // -----------------------------------------------------------------------

    private sealed class AiEvaluationItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("matchingScore")]
        public decimal? MatchingScore { get; set; }

        [JsonPropertyName("matchingReason")]
        public string? MatchingReason { get; set; }
    }
}
