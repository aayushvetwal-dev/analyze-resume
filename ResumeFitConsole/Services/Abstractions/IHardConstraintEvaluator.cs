using ResumeFitConsole.Core.Models;

namespace ResumeFitConsole.Services.Abstractions;

internal interface IHardConstraintEvaluator
{
    Task<IReadOnlyList<HardConstraintResult>> EvaluateAsync(
        IReadOnlyList<string> requirementItems,
        IReadOnlyList<string> resumeItems,
        IReadOnlyCollection<string> requiredCategories,
        CancellationToken cancellationToken = default);
}
