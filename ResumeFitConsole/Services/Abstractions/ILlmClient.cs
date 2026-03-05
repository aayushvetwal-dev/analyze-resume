namespace ResumeFitConsole.Services.Abstractions;

internal interface ILlmClient
{
    Task<string> GenerateTextAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> EmbedAsync(string model, IReadOnlyList<string> inputs, CancellationToken cancellationToken = default);
}
