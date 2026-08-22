using System.Text;

using CastoPet.Core.Product;

namespace CastoPet.Core.Diagnostics;

public enum CrashReportKind
{
    Fatal,
    UnobservedTask,
}

public sealed record CrashReportContext(
    DateTimeOffset TimestampUtc,
    string AppVersion,
    string OperatingSystem,
    string ProcessArchitecture,
    string UserProfilePath,
    string UserName,
    string ProductEdition = CastoPetBuildInfo.UnknownValue,
    string SourceCommit = CastoPetBuildInfo.UnknownValue,
    CrashReportKind ReportKind = CrashReportKind.Fatal);

public static class CrashReportFormatter
{
    public const int MaxLogTailLines = 80;

    public static string Format(
        CrashReportContext context,
        Exception exception,
        IReadOnlyList<string> logLines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("CastoPet local crash report");
        builder.AppendLine($"Timestamp UTC: {context.TimestampUtc:O}");
        builder.AppendLine($"CastoPet version: {context.AppVersion}");
        builder.AppendLine($"CastoPet edition: {context.ProductEdition}");
        builder.AppendLine($"Source commit: {context.SourceCommit}");
        builder.AppendLine($"Report kind: {context.ReportKind}");
        builder.AppendLine($"Operating system: {context.OperatingSystem}");
        builder.AppendLine($"Process architecture: {context.ProcessArchitecture}");
        builder.AppendLine();
        builder.AppendLine("Exception:");
        builder.AppendLine(exception.ToString());

        if (logLines.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"Recent application log (last {MaxLogTailLines} lines maximum):");
            foreach (var line in logLines.Skip(Math.Max(0, logLines.Count - MaxLogTailLines)))
            {
                builder.AppendLine(line);
            }
        }

        return Sanitize(builder.ToString(), context);
    }

    private static string Sanitize(string value, CrashReportContext context)
    {
        var sanitized = value;
        if (!string.IsNullOrWhiteSpace(context.UserProfilePath))
        {
            sanitized = sanitized.Replace(
                context.UserProfilePath,
                "%USERPROFILE%",
                StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(context.UserName))
        {
            sanitized = sanitized.Replace(
                context.UserName,
                "%USERNAME%",
                StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
    }
}
