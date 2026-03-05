using ResumeFitConsole.Core.Models;
using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Services.Constraints;

internal sealed class DeterministicEligibilityEvaluator : IEligibilityEvaluator
{
    public Task<EligibilityDecision> EvaluateAsync(
        IReadOnlyList<HardConstraintResult> constraints,
        IReadOnlyCollection<string> requiredCategories,
        CancellationToken cancellationToken = default)
    {
        if (requiredCategories.Count == 0)
        {
            return Task.FromResult(new EligibilityDecision(
                EligibilityStatus.Eligible,
                new[] { "No required hard constraints configured." }));
        }

        var blockers = constraints
            .Where(item => requiredCategories.Contains(item.Category, StringComparer.OrdinalIgnoreCase) && item.Status != HardConstraintStatus.Met)
            .Select(item => $"{item.Category}: {item.Status} ({item.Notes})")
            .ToList();

        var missingCategories = requiredCategories
            .Where(required => constraints.All(item => !item.Category.Equals(required, StringComparison.OrdinalIgnoreCase)))
            .Select(required => $"{required}: Unknown (No matching constraint evaluation found.)")
            .ToList();

        blockers.AddRange(missingCategories);

        if (blockers.Count > 0)
        {
            return Task.FromResult(new EligibilityDecision(EligibilityStatus.NotEligible, blockers));
        }

        return Task.FromResult(new EligibilityDecision(
            EligibilityStatus.Eligible,
            new[] { "All required hard constraints are met." }));
    }
}
