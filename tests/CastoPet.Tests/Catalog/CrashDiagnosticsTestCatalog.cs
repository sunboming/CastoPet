namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> CrashDiagnosticsTestCases { get; } =
    [
        new("Crash reports sanitize user paths and include exception chains", CrashReportsSanitizeUserPathsAndIncludeExceptionChains),
        new("Crash reports include edition and source commit", CrashReportsIncludeEditionAndSourceCommit),
        new("Build information parses SDK source revisions", BuildInformationParsesSdkSourceRevisions),
        new("Crash reports keep a bounded log tail", CrashReportsKeepABoundedLogTail),
        new("Crash report service writes and acknowledges reports", CrashReportServiceWritesAndAcknowledgesReports),
        new("Diagnostic reports do not trigger crash notifications", DiagnosticReportsDoNotTriggerCrashNotifications),
        new("Crash report service contains file system failures", CrashReportServiceContainsFileSystemFailures),
        new("Crash report service prunes old reports", CrashReportServicePrunesOldReports),
        new("Crash report retention orders fatal and diagnostic reports together", CrashReportRetentionOrdersFatalAndDiagnosticReportsTogether),
        new("Unobserved tasks do not consume the fatal crash quota", UnobservedTasksDoNotConsumeTheFatalCrashQuota),
        new("Application registers all unhandled exception sources", ApplicationRegistersAllUnhandledExceptionSources),
        new("Application cancels automatic update work on exit", ApplicationCancelsAutomaticUpdateWorkOnExit),
        new("Crash notification is local only", CrashNotificationIsLocalOnly),
    ];
}
