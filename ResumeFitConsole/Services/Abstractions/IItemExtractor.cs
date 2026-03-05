namespace ResumeFitConsole.Services.Abstractions;

internal interface IItemExtractor
{
    Task<IReadOnlyList<string>> ExtractItemsAsync(string text, string label, CancellationToken cancellationToken = default);
}
