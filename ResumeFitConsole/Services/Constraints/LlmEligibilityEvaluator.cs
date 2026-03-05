using System.Text;
using System.Text.Json;
using ResumeFitConsole.Core.Models;
using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Services.Constraints;

internal sealed class LlmEligibilityEvaluator : IEligibilityEvaluator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILlmClient _llmClient;
    private readonly string _model;

    public LlmEligibilityEvaluator(ILlmClient llmClient, string model)
    {
        _llmClient = llmClient;
        _model = model;
    }

    public async Task<EligibilityDecision> EvaluateAsync(
        IReadOnlyList<HardConstraintResult> constraints,
        IReadOnlyCollection<string> requiredCategories,
        CancellationToken cancellationToken = default)
    {
        var constraintsBlock = ToConstraintBlock(constraints);
        var requiredBlock = requiredCategories.Count == 0
            ? "<none>"
            : string.Join(", ", requiredCategories);

        var systemPrompt = "You decide candidate eligibility from evaluated hard constraints. Return only strict JSON.";
        var userPrompt = $$"""
You are given hard-constraint evaluation results from a hiring pipeline.
Decide final eligibility.

Return JSON object:
{
  "status": "Eligible|NotEligible",
  "reasons": ["short reason", "short reason"]
}

Rules:
1) Use required categories as the gating set. If a required category is missing or not Met, result should usually be NotEligible.
2) Be conservative: Unknown on a required category should be treated as blocker unless there is explicit equivalent evidence in provided constraints.
3) Reasons must be concise and decision-focused.
4) If required categories are empty, status should be Eligible unless constraints contain explicit disqualifying information.
5) Output valid JSON only.

Required categories:
{{requiredBlock}}

Hard constraints:
{{constraintsBlock}}
""";

        var raw = await _llmClient.GenerateTextAsync(_model, systemPrompt, userPrompt, cancellationToken);
        var json = ExtractJson(raw);

        var parsed = JsonSerializer.Deserialize<EligibilityResponse>(json, SerializerOptions);
        if (parsed is null)
        {
            throw new InvalidOperationException("Eligibility response could not be parsed.");
        }

        var status = ParseStatus(parsed.Status);
        var reasons = parsed.Reasons?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList()
            ?? new List<string>();

        if (reasons.Count == 0)
        {
            reasons.Add("No eligibility rationale returned by model.");
        }

        return new EligibilityDecision(status, reasons);
    }

    private static string ToConstraintBlock(IReadOnlyList<HardConstraintResult> constraints)
    {
        if (constraints.Count == 0)
        {
            return "<none>";
        }

        var builder = new StringBuilder();
        for (var index = 0; index < constraints.Count; index++)
        {
            var item = constraints[index];
            builder.Append('[').Append(index + 1).Append("] ")
                .Append("Category=").Append(item.Category).Append("; ")
                .Append("Status=").Append(item.Status).Append("; ")
                .Append("Requirement=").Append(item.Requirement).Append("; ")
                .Append("Evidence=").Append(string.IsNullOrWhiteSpace(item.BestEvidence) ? "<none>" : item.BestEvidence).Append("; ")
                .Append("Notes=").Append(item.Notes)
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string ExtractJson(string raw)
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

        throw new InvalidOperationException("Eligibility response did not contain valid JSON.");
    }

    private static EligibilityStatus ParseStatus(string? value)
    {
        if (string.Equals(value, "Eligible", StringComparison.OrdinalIgnoreCase))
        {
            return EligibilityStatus.Eligible;
        }

        return EligibilityStatus.NotEligible;
    }

    private sealed record EligibilityResponse(string Status, IReadOnlyList<string> Reasons);
}
