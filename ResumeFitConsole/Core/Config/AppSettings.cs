namespace ResumeFitConsole.Core.Config;

internal sealed record AppSettings(
    string ApiKey,
    string BaseUrl,
    string ChatModel,
    string EmbeddingModel,
    int TimeoutSeconds,
    double MatchThreshold,
    IReadOnlyList<string> RequiredHardConstraints,
    string EligibilityMode)
{
    public static AppSettings FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY") ?? string.Empty;
        var baseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://api.openai.com/v1";
        var chatModel = Environment.GetEnvironmentVariable("LLM_CHAT_MODEL") ?? "gpt-5-mini-2025-08-07"; // "gpt-4.1-mini";
        var embeddingModel = Environment.GetEnvironmentVariable("LLM_EMBED_MODEL") ?? "text-embedding-3-large";

        var timeoutSeconds = ParseInt(Environment.GetEnvironmentVariable("LLM_TIMEOUT_SECONDS"), 120);
        var matchThreshold = ParseDouble(Environment.GetEnvironmentVariable("MATCH_THRESHOLD"), 0.50);
        var requiredHardConstraints = ParseCsv(
            Environment.GetEnvironmentVariable("REQUIRED_HARD_CONSTRAINTS"),
            Array.Empty<string>());
        var eligibilityMode = ParseEligibilityMode(Environment.GetEnvironmentVariable("ELIGIBILITY_MODE"));

        return new AppSettings(apiKey, baseUrl.TrimEnd('/'), chatModel, embeddingModel, timeoutSeconds, matchThreshold, requiredHardConstraints, eligibilityMode);
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
    }

    private static double ParseDouble(string? value, double fallback)
    {
        return double.TryParse(value, out var parsed) && parsed > 0 && parsed <= 1.0 ? parsed : fallback;
    }

    private static IReadOnlyList<string> ParseCsv(string? value, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var parsed = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parsed.Length == 0 ? fallback : parsed;
    }

    private static string ParseEligibilityMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "llm";
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "llm" => "llm",
            "strict" => "strict",
            "deterministic" => "strict",
            "strict-deterministic" => "strict",
            _ => "llm"
        };
    }
}
