using ResumeFitConsole.Core.Config;
using ResumeFitConsole.Infrastructure.Llm;
using ResumeFitConsole.Services.Abstractions;
using ResumeFitConsole.Services.Constraints;
using ResumeFitConsole.Services.Embeddings;
using ResumeFitConsole.Services.Extraction;
using ResumeFitConsole.Services.Scoring;
using ResumeFitConsole.Utilities;
    
Directory.SetCurrentDirectory(FileLocator.GetProjectDirectory());

Console.WriteLine(AppContext.BaseDirectory);
var requirementPath = FileLocator.ResolvePath(args.ElementAtOrDefault(0), "Requirement", "requirement.md");
var resumePath = FileLocator.ResolvePath(args.ElementAtOrDefault(1), "Resume", "Resume.txt");

if (!File.Exists(requirementPath) || !File.Exists(resumePath))
{
    Console.WriteLine("Input files not found.");
    Console.WriteLine($"Requirement file: {requirementPath}");
    Console.WriteLine($"Resume file: {resumePath}");
    Console.WriteLine("Usage: dotnet run -- <requirementFilePath> <resumeFilePath>");
    return;
}

var settings = AppSettings.FromEnvironment();
if (string.IsNullOrWhiteSpace(settings.ApiKey))
{
    Console.WriteLine("Missing API key. Set environment variable LLM_API_KEY.");
    Console.WriteLine("Optional env vars: LLM_BASE_URL, LLM_CHAT_MODEL, LLM_EMBED_MODEL, LLM_TIMEOUT_SECONDS, MATCH_THRESHOLD, REQUIRED_HARD_CONSTRAINTS, ELIGIBILITY_MODE.");
    return;
}

var requirementText = await File.ReadAllTextAsync(requirementPath);
var resumeText = await File.ReadAllTextAsync(resumePath);

using var llmClient = new OpenAiCompatibleLlmClient(settings);
var extractor = new LlmItemExtractor(llmClient, settings.ChatModel);
var embedder = new LlmEmbeddingProvider(llmClient, settings.EmbeddingModel);
var constraintEvaluator = new LlmHardConstraintEvaluator(llmClient, settings.ChatModel);
IEligibilityEvaluator eligibilityEvaluator = settings.EligibilityMode == "strict"
    ? new DeterministicEligibilityEvaluator()
    : new LlmEligibilityEvaluator(llmClient, settings.ChatModel);

var analyzer = new ResumeFitAnalyzer(
    extractor,
    embedder,
    constraintEvaluator,
    eligibilityEvaluator,
    settings.MatchThreshold,
    settings.RequiredHardConstraints);

try
{
    var report = await analyzer.AnalyzeAsync(requirementText, resumeText);
    ConsoleReportPrinter.Print(report);
}
catch (Exception ex)
{
    Console.WriteLine("Analysis failed.");
    Console.WriteLine(ex.Message);
}
