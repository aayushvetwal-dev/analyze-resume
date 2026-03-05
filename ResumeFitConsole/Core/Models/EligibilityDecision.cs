namespace ResumeFitConsole.Core.Models;

internal sealed record EligibilityDecision(
    EligibilityStatus Status,
    IReadOnlyList<string> Reasons);
