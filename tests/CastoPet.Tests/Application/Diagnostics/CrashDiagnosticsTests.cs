namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void CrashReportsSanitizeUserPathsAndIncludeExceptionChains()
    {
        var context = new CrashReportContext(
            TimestampUtc: new DateTimeOffset(2026, 7, 11, 8, 30, 0, TimeSpan.Zero),
            AppVersion: "0.1.0",
            OperatingSystem: "Windows 11",
            ProcessArchitecture: "X64",
            UserProfilePath: @"C:\Users\lemon",
            UserName: "lemon");
        var exception = new InvalidOperationException(
            @"Failed at C:\Users\lemon\Documents\CastoPet",
            new IOException("inner failure"));

        var report = CrashReportFormatter.Format(context, exception, Array.Empty<string>());

        Assert.Contains(report, "2026-07-11T08:30:00.0000000+00:00", "Report should include the UTC timestamp.");
        Assert.Contains(report, "CastoPet version: 0.1.0", "Report should include the application version.");
        Assert.Contains(report, "InvalidOperationException", "Report should include the outer exception.");
        Assert.Contains(report, "IOException", "Report should include the inner exception.");
        Assert.Contains(report, "%USERPROFILE%", "User profile paths should use a neutral placeholder.");
        Assert.False(report.Contains("lemon", StringComparison.OrdinalIgnoreCase), "Report should not contain the Windows username.");
    }

    static void CrashReportsIncludeEditionAndSourceCommit()
    {
        var context = new CrashReportContext(
            TimestampUtc: new DateTimeOffset(2026, 8, 13, 2, 0, 0, TimeSpan.Zero),
            AppVersion: "0.2.0-preview.3",
            OperatingSystem: "Windows 11",
            ProcessArchitecture: "X64",
            UserProfilePath: @"C:\Users\TestUser",
            UserName: "TestUser",
            ProductEdition: "Preview",
            SourceCommit: "0123456789abcdef0123456789abcdef01234567",
            ReportKind: CrashReportKind.Fatal);

        var report = CrashReportFormatter.Format(context, new Exception("failure"), []);

        Assert.Contains(report, "CastoPet edition: Preview", "Crash reports should identify Stable versus Preview.");
        Assert.Contains(report, "Source commit: 0123456789abcdef0123456789abcdef01234567", "Crash reports should identify the exact source revision.");
        Assert.Contains(report, "Report kind: Fatal", "Crash reports should distinguish fatal failures from diagnostics.");
    }

    static void BuildInformationParsesSdkSourceRevisions()
    {
        var preview = CastoPetBuildInfo.Parse(
            CastoPetEdition.Preview,
            "0.2.0-preview.3+0123456789abcdef0123456789abcdef01234567",
            "0.2.0");
        var stable = CastoPetBuildInfo.Parse(CastoPetEdition.Stable, "0.1.0", "0.1.0");

        Assert.Equal("0.2.0-preview.3", preview.Version, "The semantic version should exclude build metadata.");
        Assert.Equal(CastoPetEdition.Preview, preview.Edition, "The build edition should come from the compiled feature profile.");
        Assert.Equal("0123456789abcdef0123456789abcdef01234567", preview.SourceCommit, "The SDK source revision should be preserved in full.");
        Assert.Equal("unknown", stable.SourceCommit, "Direct builds without source metadata should use an explicit fallback.");
    }

    static void CrashReportsKeepABoundedLogTail()
    {
        var context = new CrashReportContext(
            DateTimeOffset.UtcNow,
            "0.1.0",
            "Windows",
            "X64",
            @"C:\Users\TestUser",
            "TestUser");
        var lines = Enumerable.Range(0, 100).Select(index => $"log-{index:000}").ToArray();

        var report = CrashReportFormatter.Format(context, new Exception("failure"), lines);

        Assert.False(report.Contains("log-019", StringComparison.Ordinal), "Old log lines should be excluded.");
        Assert.Contains(report, "log-020", "The last 80 log lines should be included.");
        Assert.Contains(report, "log-099", "The newest log line should be included.");
    }

    static void CrashReportServiceWritesAndAcknowledgesReports()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            buildInfo: new CastoPetBuildInfo(
                "0.2.0-preview.3",
                CastoPetEdition.Preview,
                "0123456789abcdef0123456789abcdef01234567"));

        var written = service.TryWriteReport(new InvalidOperationException("test crash"), out var report);

        Assert.True(written, "Crash report write should succeed in a writable data directory.");
        Assert.True(report is not null, "A successful write should return report metadata.");
        Assert.True(File.Exists(report!.Path), "Crash report metadata should point to the written file.");
        var content = File.ReadAllText(report.Path);
        Assert.Contains(content, "CastoPet edition: Preview", "The service should pass its compiled edition into the report.");
        Assert.Contains(content, "Source commit: 0123456789abcdef0123456789abcdef01234567", "The service should pass its source revision into the report.");
        Assert.Equal(report.Id, System.IO.Path.GetFileNameWithoutExtension(report.Path), "Report ID should match its filename.");
        Assert.Equal(report.Id, service.GetLatestUnacknowledged(null)?.Id, "An unacknowledged report should be discovered.");
        Assert.True(service.GetLatestUnacknowledged(report.Id) is null, "Acknowledged reports should not be returned again.");
    }

    static void DiagnosticReportsDoNotTriggerCrashNotifications()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new CrashReportService(paths, new LoggingService(paths));

        var written = service.TryWriteReport(
            new AggregateException("unobserved task"),
            CrashReportKind.UnobservedTask,
            out var report);

        Assert.True(written, "Unobserved task diagnostics should still be persisted locally.");
        Assert.True(report is not null && report.Id.StartsWith("diagnostic-", StringComparison.Ordinal), "Non-fatal reports should use a diagnostic identity.");
        Assert.True(service.GetLatestUnacknowledged(null) is null, "A diagnostic report should not be presented as a previous application crash.");
    }

    static void CrashReportServiceContainsFileSystemFailures()
    {
        using var temp = TempDirectory.Create();
        var blockedDataPath = System.IO.Path.Combine(temp.Path, "blocked");
        File.WriteAllText(blockedDataPath, "not a directory");
        var paths = new AppPaths(blockedDataPath);
        var service = new CrashReportService(paths, new LoggingService(paths));

        var written = service.TryWriteReport(new Exception("failure"), out var report);

        Assert.False(written, "Crash report failures should be contained.");
        Assert.True(report is null, "Failed writes should not return report metadata.");
    }

    static void CrashReportServicePrunesOldReports()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var timestamp = new DateTimeOffset(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);
        var nextReport = -1;
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            maxReports: 3,
            nowProvider: () => timestamp.AddMilliseconds(Interlocked.Increment(ref nextReport)));

        for (var index = 0; index < 5; index++)
        {
            Assert.True(service.TryWriteReport(new Exception($"failure-{index}"), out _), "Crash report write should succeed.");
        }

        var reports = Directory.EnumerateFiles(paths.CrashesDirectory, "crash-*.txt").Order().ToArray();
        Assert.Equal(3, reports.Length, "Crash retention should keep only the configured number of reports.");
        Assert.False(File.ReadAllText(reports[0]).Contains("failure-0", StringComparison.Ordinal), "The oldest report should be pruned first.");
        Assert.Contains(File.ReadAllText(reports[^1]), "failure-4", "The newest report should remain available.");
    }

    static void CrashReportRetentionOrdersFatalAndDiagnosticReportsTogether()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var timestamp = new DateTimeOffset(2026, 8, 13, 3, 0, 0, TimeSpan.Zero);
        var nextReport = -1;
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            maxReports: 2,
            nowProvider: () => timestamp.AddSeconds(Interlocked.Increment(ref nextReport)));

        Assert.True(service.TryWriteReport(new Exception("old diagnostic"), CrashReportKind.UnobservedTask, out _), "The diagnostic should be written.");
        Assert.True(service.TryWriteReport(new Exception("middle fatal"), out _), "The first fatal report should be written.");
        Assert.True(service.TryWriteReport(new Exception("new fatal"), out _), "The latest fatal report should be written.");

        var reports = Directory.EnumerateFiles(paths.CrashesDirectory, "*.txt").ToArray();
        Assert.Equal(2, reports.Length, "Retention should apply one shared chronological budget.");
        Assert.False(reports.Any(path => System.IO.Path.GetFileName(path).StartsWith("diagnostic-", StringComparison.Ordinal)), "The oldest diagnostic should be pruned before newer fatal reports.");
    }

    static void UnobservedTasksDoNotConsumeTheFatalCrashQuota()
    {
        var recordedKinds = new List<CrashReportKind>();
        var capture = new CrashCaptureCoordinator((_, kind) =>
        {
            recordedKinds.Add(kind);
            return true;
        });
        var unobserved = new UnobservedTaskExceptionEventArgs(
            new AggregateException(new InvalidOperationException("background failure")));

        capture.HandleUnobservedTaskException(unobserved);
        var firstFatal = capture.TryRecordFatal(new InvalidOperationException("fatal failure"));
        var duplicateFatal = capture.TryRecordFatal(new InvalidOperationException("duplicate fatal failure"));

        Assert.True(unobserved.Observed, "Handled task exceptions should always be marked observed.");
        Assert.True(firstFatal, "A later fatal exception should retain the one available fatal report slot.");
        Assert.False(duplicateFatal, "Only one fatal exception should be persisted during a process lifetime.");
        Assert.Equal(2, recordedKinds.Count, "One diagnostic and one fatal report should be written.");
        Assert.Equal(CrashReportKind.UnobservedTask, recordedKinds[0], "The task failure should be classified as non-fatal.");
        Assert.Equal(CrashReportKind.Fatal, recordedKinds[1], "The fatal failure should use the independent fatal gate.");
    }

    static void ApplicationRegistersAllUnhandledExceptionSources()
    {
        var workspace = FindWorkspaceRoot();
        var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(appSource, "DispatcherUnhandledException", "WPF dispatcher exceptions should be recorded.");
        Assert.Contains(appSource, "AppDomain.CurrentDomain.UnhandledException", "Non-UI fatal exceptions should be recorded.");
        Assert.Contains(appSource, "TaskScheduler.UnobservedTaskException", "Unobserved task exceptions should be recorded.");
    }

    static void ApplicationCancelsAutomaticUpdateWorkOnExit()
    {
        var workspace = FindWorkspaceRoot();
        var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(appSource, "_applicationLifetime.Cancel()", "Application exit should cancel pending background work.");
        Assert.Contains(appSource, "Task.Delay(TimeSpan.FromSeconds(10), cancellationToken)", "Startup update delay should observe application cancellation.");
        Assert.Contains(appSource, "CheckAsync(manual: false, cancellationToken)", "Automatic update checks should observe application cancellation.");
    }

    static void CrashNotificationIsLocalOnly()
    {
        var workspace = FindWorkspaceRoot();
        var xamlPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "CrashNotificationWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains(xaml, "打开日志目录", "Crash notification should provide local report access.");
        Assert.Contains(xaml, "忽略", "Crash notification should support acknowledgement.");
        Assert.False(xaml.Contains("上传", StringComparison.Ordinal), "Crash notification should not imply network upload.");
    }

}
