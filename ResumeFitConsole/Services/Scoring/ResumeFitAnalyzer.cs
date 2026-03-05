using ResumeFitConsole.Core.Models;
using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Services.Scoring;

internal sealed class ResumeFitAnalyzer
{
    private readonly IItemExtractor _itemExtractor;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IHardConstraintEvaluator _hardConstraintEvaluator;
    private readonly IEligibilityEvaluator _eligibilityEvaluator;
    private readonly double _matchThreshold;
    private readonly HashSet<string> _requiredHardConstraintCategories;

    public ResumeFitAnalyzer(
        IItemExtractor itemExtractor,
        IEmbeddingProvider embeddingProvider,
        IHardConstraintEvaluator hardConstraintEvaluator,
        IEligibilityEvaluator eligibilityEvaluator,
        double matchThreshold,
        IReadOnlyList<string> requiredHardConstraintCategories)
    {
        _itemExtractor = itemExtractor;
        _embeddingProvider = embeddingProvider;
        _hardConstraintEvaluator = hardConstraintEvaluator;
        _eligibilityEvaluator = eligibilityEvaluator;
        _matchThreshold = matchThreshold;
        _requiredHardConstraintCategories = new HashSet<string>(requiredHardConstraintCategories, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AnalysisReport> AnalyzeAsync(string requirementText, string resumeText, CancellationToken cancellationToken = default)
    {
        var requirementItems = await _itemExtractor.ExtractItemsAsync(requirementText, "requirements", cancellationToken);
        var resumeItems = await _itemExtractor.ExtractItemsAsync(resumeText, "resume", cancellationToken);

        var requirementVectors = await _embeddingProvider.CreateEmbeddingsAsync(requirementItems, cancellationToken);
        var resumeVectors = await _embeddingProvider.CreateEmbeddingsAsync(resumeItems, cancellationToken);

        var matches = new List<RequirementMatch>(requirementItems.Count);

        for (var requirementIndex = 0; requirementIndex < requirementItems.Count; requirementIndex++)
        {
            var bestScore = -1.0;
            var bestResumeIndex = -1;

            for (var resumeIndex = 0; resumeIndex < resumeItems.Count; resumeIndex++)
            {
                var similarity = CosineSimilarity.Calculate(requirementVectors[requirementIndex], resumeVectors[resumeIndex]);
                if (similarity > bestScore)
                {
                    bestScore = similarity;
                    bestResumeIndex = resumeIndex;
                }
            }

            matches.Add(new RequirementMatch(
                requirementItems[requirementIndex],
                bestResumeIndex >= 0 ? resumeItems[bestResumeIndex] : string.Empty,
                Math.Max(0, bestScore),
                bestResumeIndex));
        }

        var covered = matches.Count(item => item.Similarity >= _matchThreshold);
        var coverageRatio = matches.Count == 0 ? 0.0 : (double)covered / matches.Count;
        var averageSimilarity = matches.Count == 0 ? 0.0 : matches.Average(item => item.Similarity);
        var compositeFitScore = averageSimilarity * 100.0;
        var hardConstraintResults = await _hardConstraintEvaluator.EvaluateAsync(
            requirementItems,
            resumeItems,
            _requiredHardConstraintCategories,
            cancellationToken);
        var metHardConstraints = hardConstraintResults.Count(item => item.Status == HardConstraintStatus.Met);
        var eligibilityDecision = await _eligibilityEvaluator.EvaluateAsync(
            hardConstraintResults,
            _requiredHardConstraintCategories,
            cancellationToken);

        return new AnalysisReport(
            requirementItems,
            resumeItems,
            matches.OrderByDescending(item => item.Similarity).ToList(),
            hardConstraintResults,
            metHardConstraints,
            hardConstraintResults.Count,
            eligibilityDecision.Status,
            eligibilityDecision.Reasons,
            _matchThreshold,
            covered,
            coverageRatio,
            averageSimilarity,
            compositeFitScore);
    }
}
