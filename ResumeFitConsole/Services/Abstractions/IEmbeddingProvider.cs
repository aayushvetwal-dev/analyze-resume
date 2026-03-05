namespace ResumeFitConsole.Services.Abstractions;

internal interface IEmbeddingProvider
{
    Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyList<string> items, CancellationToken cancellationToken = default);
}
