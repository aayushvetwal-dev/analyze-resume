namespace ResumeFitConsole.Core.Models;

internal sealed record RequirementMatch(
    string Requirement,
    string BestResumeEvidence,
    double Similarity,
    int ResumeItemIndex);
