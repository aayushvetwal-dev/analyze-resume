namespace ResumeFitConsole.Utilities;

internal static class FileLocator
{
    public static string GetProjectDirectory()
    {
        var fromCurrent = FindDirectoryContainingProjectFile(new DirectoryInfo(Directory.GetCurrentDirectory()));
        if (fromCurrent is not null)
        {
            return fromCurrent;
        }

        var fromBase = FindDirectoryContainingProjectFile(new DirectoryInfo(AppContext.BaseDirectory));
        if (fromBase is not null)
        {
            return fromBase;
        }

        return Directory.GetCurrentDirectory();
    }

    public static string ResolvePath(string? providedPath, params string[] fallbackParts)
    {
        if (!string.IsNullOrWhiteSpace(providedPath))
        {
            return Path.GetFullPath(providedPath);
        }

        var currentDirectory = new DirectoryInfo(GetProjectDirectory());
        while (currentDirectory is not null)
        {
            var candidate = Path.Combine(currentDirectory.FullName, Path.Combine(fallbackParts));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return Path.GetFullPath(Path.Combine(GetProjectDirectory(), Path.Combine(fallbackParts)));
    }

    private static string? FindDirectoryContainingProjectFile(DirectoryInfo? start)
    {
        var current = start;
        while (current is not null)
        {
            if (current.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
