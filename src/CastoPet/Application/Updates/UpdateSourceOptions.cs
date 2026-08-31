namespace CastoPet.Application.Updates;

public static class UpdateSourceOptions
{
    public const string TestSourceArgument = "--test-update-source";

    public static string? Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var optionIndex = -1;
        for (var index = 0; index < arguments.Count; index++)
        {
            if (!string.Equals(arguments[index], TestSourceArgument, StringComparison.Ordinal))
            {
                continue;
            }

            if (optionIndex >= 0)
            {
                throw new ArgumentException($"{TestSourceArgument} may be specified only once.", nameof(arguments));
            }
            optionIndex = index;
        }

        if (optionIndex < 0)
        {
            return null;
        }
        if (optionIndex + 1 >= arguments.Count || string.IsNullOrWhiteSpace(arguments[optionIndex + 1]))
        {
            throw new ArgumentException($"{TestSourceArgument} requires a directory path.", nameof(arguments));
        }

        var path = arguments[optionIndex + 1];
        if (!System.IO.Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException($"{TestSourceArgument} requires an absolute directory path.", nameof(arguments));
        }

        var fullPath = System.IO.Path.GetFullPath(path);
        if (!System.IO.Directory.Exists(fullPath))
        {
            throw new System.IO.DirectoryNotFoundException($"Test update source '{fullPath}' does not exist.");
        }

        return fullPath;
    }
}
