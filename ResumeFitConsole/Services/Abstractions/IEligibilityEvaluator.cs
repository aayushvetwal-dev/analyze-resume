using ResumeFitConsole.Core.Models;

namespace ResumeFitConsole.Services.Abstractions;

internal interface IEligibilityEvaluator
{
    Task<EligibilityDecision> EvaluateAsync(
        IReadOnlyList<HardConstraintResult> constraints,
        IReadOnlyCollection<string> requiredCategories,
        CancellationToken cancellationToken = default);
}
