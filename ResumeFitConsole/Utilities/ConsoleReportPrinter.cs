using ResumeFitConsole.Core.Models;

namespace ResumeFitConsole.Utilities;

internal static class ConsoleReportPrinter
{
    public static void Print(AnalysisReport report)
    {
        Console.WriteLine("=== Extracted Requirement Items ===");
        PrintIndexed(report.RequirementItems);

        Console.WriteLine();
        Console.WriteLine("=== Extracted Resume Evidence Items ===");
        PrintIndexed(report.ResumeItems);

        Console.WriteLine();
        Console.WriteLine("=== Requirement-to-Resume Matches ===");
        foreach (var match in report.Matches.Where(m => m.Similarity >= report.matchThreshold).OrderByDescending(m => m.Similarity))
        {
            Console.WriteLine($"Requirement: {match.Requirement}");
            Console.WriteLine($"Best Evidence: {match.BestResumeEvidence}");
            Console.WriteLine($"Cosine Similarity: {match.Similarity:F4}");
            Console.WriteLine(new string('-', 80));
        }

        Console.WriteLine();
        Console.WriteLine("=== Hard Constraints (Pass/Fail) ===");
        if (report.HardConstraints.Count == 0)
        {
            Console.WriteLine("No hard constraints detected in requirements.");
        }
        else
        {
            foreach (var constraint in report.HardConstraints)
            {
                Console.WriteLine($"Category: {constraint.Category}");
                Console.WriteLine($"Requirement: {constraint.Requirement}");
                Console.WriteLine($"Status: {constraint.Status}");
                Console.WriteLine($"Evidence: {(string.IsNullOrWhiteSpace(constraint.BestEvidence) ? "<none>" : constraint.BestEvidence)}");
                Console.WriteLine($"Notes: {constraint.Notes}");
                Console.WriteLine(new string('-', 80));
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== Summary Metrics ===");
        Console.WriteLine($"Requirement Count: {report.RequirementItems.Count}");
        Console.WriteLine($"Resume Evidence Count: {report.ResumeItems.Count}");
        Console.WriteLine($"Hard Constraints Met: {report.MetHardConstraints}/{report.TotalHardConstraints}");
        Console.WriteLine($"Overall Eligibility: {report.Eligibility}");
        if (report.EligibilityReasons.Count > 0)
        {
            Console.WriteLine("Eligibility Reasons:");
            foreach (var reason in report.EligibilityReasons)
            {
                Console.WriteLine($"- {reason}");
            }
        }
        Console.WriteLine($"Covered Requirements: {report.CoveredRequirements}");
        Console.WriteLine($"Coverage Ratio: {report.CoverageRatio:P2}");
        Console.WriteLine($"Average Similarity: {report.AverageSimilarity:F4}");
        Console.WriteLine($"Composite Fit Score: {report.CompositeFitScore:F2}/100");
    }

    private static void PrintIndexed(IReadOnlyList<string> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            Console.WriteLine($"[{index + 1}] {items[index]}");
        }
    }
}
