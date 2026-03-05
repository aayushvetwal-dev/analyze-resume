namespace ResumeFitConsole.Core.Models;

internal enum HardConstraintStatus
{
    Met,
    NotMet,
    Unknown
}

internal sealed record HardConstraintResult(
    string Requirement,
    string Category,
    HardConstraintStatus Status,
    string BestEvidence,
    string Notes);
