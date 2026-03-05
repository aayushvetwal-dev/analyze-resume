using System.Text;
using System.Text.Json;
using ResumeFitConsole.Core.Models;
using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Services.Constraints;

internal sealed class LlmHardConstraintEvaluator : IHardConstraintEvaluator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILlmClient _llmClient;
    private readonly string _model;

    public LlmHardConstraintEvaluator(ILlmClient llmClient, string model)
    {
        _llmClient = llmClient;
        _model = model;
    }

    public async Task<IReadOnlyList<HardConstraintResult>> EvaluateAsync(
        IReadOnlyList<string> requirementItems,
        IReadOnlyList<string> resumeItems,
        IReadOnlyCollection<string> requiredCategories,
        CancellationToken cancellationToken = default)
    {
        if (requirementItems.Count == 0)
        {
            return BuildUnknownForRequiredCategories(requiredCategories);
        }

        var requirementBlock = ToIndexedList(requirementItems);
        var resumeBlock = ToIndexedList(resumeItems);
        var requiredCategoriesBlock = requiredCategories.Count == 0
            ? "<none>"
            : string.Join(", ", requiredCategories);

                var systemPrompt = "You evaluate hard hiring constraints. Return only strict JSON.";
                var userPrompt = $$"""
Evaluate hard constraints from a job requirements list against resume evidence.

Return JSON object:
{
  "constraints": [
        {
      "requirement": "string",
      "category": "string",
            "normalizedCategory": "string",
      "status": "Met|NotMet|Unknown",
      "bestEvidence": "string",
      "notes": "short rationale"
        }
  ]
}

Rules:
1) Only include TRUE hard constraints (for example education, years/experience, certification, work authorization, location/onsite, legal clearance).
2) Do not include soft skills or general technical preferences as hard constraints.
3) category can be a descriptive label.
4) normalizedCategory must be canonical and stable:
    - If semantically equivalent to one of the Required categories, use that exact required category text.
    - Otherwise provide a concise canonical category label.
5) status=Met only with explicit resume evidence.
6) status=NotMet only when requirement is explicit and resume evidence contradicts or clearly falls short.
7) status=Unknown when evidence is missing or ambiguous.
8) bestEvidence must be copied/paraphrased from one resume item, or empty string when none.
9) Evaluate all hard constraints you can detect from requirements.

Required categories for downstream eligibility gate:
{{requiredCategoriesBlock}}

Requirements:
{{requirementBlock}}

Resume evidence:
{{resumeBlock}}
""";

        var raw = await _llmClient.GenerateTextAsync(_model, systemPrompt, userPrompt, cancellationToken);
        var json = ExtractJson(raw);

        var parsed = JsonSerializer.Deserialize<ConstraintResponse>(json, SerializerOptions);
        var constraints = parsed?.Constraints?
            .Where(item => !string.IsNullOrWhiteSpace(item.Requirement)
                && (!string.IsNullOrWhiteSpace(item.NormalizedCategory) || !string.IsNullOrWhiteSpace(item.Category)))
            .Select(item => new HardConstraintResult(
                item.Requirement.Trim(),
                ResolveCategory(item, requiredCategories),
                ParseStatus(item.Status),
                item.BestEvidence?.Trim() ?? string.Empty,
                string.IsNullOrWhiteSpace(item.Notes) ? "No rationale provided." : item.Notes.Trim()))
            .ToList()
            ?? new List<HardConstraintResult>();

        EnsureRequiredCategoriesPresent(constraints, requiredCategories);
        return constraints;
    }

    private static void EnsureRequiredCategoriesPresent(List<HardConstraintResult> constraints, IReadOnlyCollection<string> requiredCategories)
    {
        foreach (var required in requiredCategories)
        {
            if (constraints.Any(item => item.Category.Equals(required, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            constraints.Add(new HardConstraintResult(
                $"Required category: {required}",
                required,
                HardConstraintStatus.Unknown,
                string.Empty,
                "No hard constraint detected for this required category."));
        }
    }

    private static IReadOnlyList<HardConstraintResult> BuildUnknownForRequiredCategories(IReadOnlyCollection<string> requiredCategories)
    {
        return requiredCategories
            .Select(required => new HardConstraintResult(
                $"Required category: {required}",
                required,
                HardConstraintStatus.Unknown,
                string.Empty,
                "No requirements were extracted to evaluate this category."))
            .ToList();
    }

    private static string ToIndexedList(IReadOnlyList<string> items)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < items.Count; index++)
        {
            builder.Append('[').Append(index + 1).Append("] ").AppendLine(items[index]);
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

        throw new InvalidOperationException("Constraint evaluation response did not contain valid JSON.");
    }

    private static HardConstraintStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return HardConstraintStatus.Unknown;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "met" => HardConstraintStatus.Met,
            "notmet" => HardConstraintStatus.NotMet,
            "not_met" => HardConstraintStatus.NotMet,
            "not met" => HardConstraintStatus.NotMet,
            _ => HardConstraintStatus.Unknown
        };
    }

    private static string ResolveCategory(ConstraintItem item, IReadOnlyCollection<string> requiredCategories)
    {
        var raw = string.IsNullOrWhiteSpace(item.NormalizedCategory)
            ? item.Category
            : item.NormalizedCategory;

        var candidate = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return "Other";
        }

        var matchedRequired = requiredCategories.FirstOrDefault(required =>
            required.Equals(candidate, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(matchedRequired) ? candidate : matchedRequired;
    }

    private sealed record ConstraintResponse(IReadOnlyList<ConstraintItem> Constraints);

    private sealed record ConstraintItem(
        string Requirement,
        string Category,
        string NormalizedCategory,
        string Status,
        string BestEvidence,
        string Notes);
}
