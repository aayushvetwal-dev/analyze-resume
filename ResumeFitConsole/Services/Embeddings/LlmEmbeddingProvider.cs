using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Services.Embeddings;

internal sealed class LlmEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILlmClient _llmClient;
    private readonly string _model;

    public LlmEmbeddingProvider(ILlmClient llmClient, string model)
    {
        _llmClient = llmClient;
        _model = model;
    }

    public Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyList<string> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<float[]>>(Array.Empty<float[]>());
        }

        return _llmClient.EmbedAsync(_model, items, cancellationToken);
    }
}
