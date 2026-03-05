using System.Text.Json;
using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Services.Extraction;

internal sealed class LlmItemExtractor : IItemExtractor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILlmClient _llmClient;
    private readonly string _model;

    public LlmItemExtractor(ILlmClient llmClient, string model)
    {
        _llmClient = llmClient;
        _model = model;
    }

    public async Task<IReadOnlyList<string>> ExtractItemsAsync(string text, string label, CancellationToken cancellationToken = default)
    {
        var systemPrompt = "You are an information extraction engine. Return only valid JSON.";
        var labelSpecificGuidance = label.Equals("requirements", StringComparison.OrdinalIgnoreCase)
            ? "Include all explicit and implied requirements, constraints, and qualifications from the document."
            : "Include all explicit evidence statements that can support or refute requirements and constraints from the document.";

        var userPrompt = $"""
Extract atomic capability statements from the {label} document.
Rules:
1) Return JSON object with one field: items (array of strings).
2) Keep each item concise and self-contained.
3) Preserve domain-specific terminology exactly.
4) Include both explicit and implied capabilities/constraints when strongly supported.
5) Deduplicate near-duplicates.
6) Do not include company culture/benefits unless clearly a requirement or evidence item.
7) Preserve measurable constraints exactly when present (numbers, dates, durations, locations, legal/eligibility terms, certifications, security or compliance requirements).
8) Do not drop hard constraints from the document.

Additional guidance:
{labelSpecificGuidance}

Document:
{text}
""";

        var raw = await _llmClient.GenerateTextAsync(_model, systemPrompt, userPrompt, cancellationToken);
        var json = TryExtractJson(raw);

        var parsed = JsonSerializer.Deserialize<ExtractedItemsResponse>(json, SerializerOptions)
            ?? throw new InvalidOperationException("LLM extraction response could not be parsed.");

        var cleaned = parsed.Items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleaned.Count == 0)
        {
            throw new InvalidOperationException($"No {label} items were extracted.");
        }

        return cleaned;
    }

    private static string TryExtractJson(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
        {
            return trimmed;
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }

        throw new InvalidOperationException("LLM response did not contain valid JSON.");
    }

    private sealed record ExtractedItemsResponse(IReadOnlyList<string> Items);
}
