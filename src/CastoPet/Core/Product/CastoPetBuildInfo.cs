using System.Reflection;

namespace CastoPet.Core.Product;

public sealed record CastoPetBuildInfo(
    string Version,
    string SourceCommit)
{
    public const string UnknownValue = "unknown";

    public static CastoPetBuildInfo Current()
    {
        var assembly = typeof(CastoPetBuildInfo).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var fallbackVersion = assembly.GetName().Version?.ToString(3) ?? UnknownValue;
        return Parse(informationalVersion, fallbackVersion);
    }

    public static CastoPetBuildInfo Parse(
        string? informationalVersion,
        string? fallbackVersion)
    {
        var value = informationalVersion?.Trim();
        var metadataSeparator = value?.IndexOf('+') ?? -1;
        var version = metadataSeparator > 0
            ? value![..metadataSeparator]
            : value;
        var metadata = metadataSeparator >= 0 && metadataSeparator < value!.Length - 1
            ? value[(metadataSeparator + 1)..]
            : null;
        var sourceCommit = metadata?
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return new CastoPetBuildInfo(
            string.IsNullOrWhiteSpace(version)
                ? string.IsNullOrWhiteSpace(fallbackVersion) ? UnknownValue : fallbackVersion
                : version,
            string.IsNullOrWhiteSpace(sourceCommit) ? UnknownValue : sourceCommit);
    }
}
