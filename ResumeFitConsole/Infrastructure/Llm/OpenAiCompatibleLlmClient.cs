using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ResumeFitConsole.Core.Config;
using ResumeFitConsole.Services.Abstractions;

namespace ResumeFitConsole.Infrastructure.Llm;

internal sealed class OpenAiCompatibleLlmClient : ILlmClient, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public OpenAiCompatibleLlmClient(AppSettings settings)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl + "/")
        };
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    public async Task<string> GenerateTextAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Chat completion failed ({(int)response.StatusCode}): {body}");
        }

        var parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(body, SerializerOptions)
            ?? throw new InvalidOperationException("Chat completion response could not be parsed.");

        var content = parsed.Choices.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Chat completion did not return message content.");
        }

        return content;
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(string model, IReadOnlyList<string> inputs, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model,
            input = inputs
        };

        using var response = await _httpClient.PostAsJsonAsync("embeddings", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embedding request failed ({(int)response.StatusCode}): {body}");
        }

        var parsed = JsonSerializer.Deserialize<EmbeddingResponse>(body, SerializerOptions)
            ?? throw new InvalidOperationException("Embedding response could not be parsed.");

        var embeddings = parsed.Data
            .OrderBy(item => item.Index)
            .Select(item => item.Embedding)
            .ToList();

        if (embeddings.Count != inputs.Count)
        {
            throw new InvalidOperationException("Embedding response count does not match input count.");
        }

        return embeddings;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);
    private sealed record ChatChoice(ChatMessage Message);
    private sealed record ChatMessage(string Content);

    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingData> Data);
    private sealed record EmbeddingData(int Index, float[] Embedding);
}
