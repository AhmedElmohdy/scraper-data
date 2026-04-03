namespace EtimadScraper.Configuration;

/// <summary>
/// Settings for the AI-based tender evaluation service.
/// Bound from appsettings.json section "TenderEvaluation".
/// </summary>
public class TenderEvaluationSettings
{
    public const string SectionName = "TenderEvaluation";

    /// <summary>Enables or disables the evaluation job. Set to false to prevent any execution.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>OpenRouter API key (required).</summary>
    public string OpenRouterApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenRouter model name, e.g. "openai/gpt-4o-mini" or "anthropic/claude-3-haiku".
    /// </summary>
    public string ModelName { get; set; } = "openai/gpt-4o-mini";

    /// <summary>
    /// The system/user prompt sent to the AI before the tender list.
    /// The tenders JSON will be appended automatically.
    /// </summary>
    public string MatchingScorePrompt { get; set; } =
        "You are an expert business development manager. Evaluate the following list of tenders against our company profile.\n" +
        "Company Profile: \"We are an integrator, service provider and consultant in the field of software, GIS, Remote sensing, AI, " +
        "Data collection from drone or Mobile Mapping. We are also experts in data management, Digital twin. " +
        "We work with different organizations, Municipalities, environment, Transport, Ministry of Energy and so on.\"\n\n" +
        "For each tender, provide a 'matchingScore' (0 to 100) indicating how well it fits our company profile, " +
        "and a 'matchingReason' (1-2 short sentences explaining why).";

    /// <summary>
    /// Tenders with a score below this threshold are stored but can be excluded
    /// from filtered queries. Default: 70.
    /// </summary>
    public int MinMatchingScore { get; set; } = 70;

    /// <summary>Number of tenders sent to the AI in a single request. Default: 50.</summary>
    public int ChunkSize { get; set; } = 50;

    /// <summary>Milliseconds to wait between consecutive AI requests. Default: 4000.</summary>
    public int DelayBetweenChunksMs { get; set; } = 4000;

    /// <summary>
    /// Maximum tokens the AI may use in its response.
    /// Increase when processing larger chunks.
    /// </summary>
    public int MaxTokens { get; set; } = 4000;
}
