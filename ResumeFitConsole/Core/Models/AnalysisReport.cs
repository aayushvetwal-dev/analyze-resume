namespace ResumeFitConsole.Core.Models;

internal enum EligibilityStatus
{
    Eligible,
    NotEligible
}

internal sealed record AnalysisReport(
    IReadOnlyList<string> RequirementItems,
    IReadOnlyList<string> ResumeItems,
    IReadOnlyList<RequirementMatch> Matches,
    IReadOnlyList<HardConstraintResult> HardConstraints,
    int MetHardConstraints,
    int TotalHardConstraints,
    EligibilityStatus Eligibility,
    IReadOnlyList<string> EligibilityReasons,
    double matchThreshold,
    int CoveredRequirements,
    double CoverageRatio,
    double AverageSimilarity,
    double CompositeFitScore);
