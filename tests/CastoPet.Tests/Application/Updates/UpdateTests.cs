namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void UpdatePolicyChecksAtMostOncePerLocalDay()
    {
        var today = new DateOnly(2026, 7, 11);

        Assert.True(UpdateCheckPolicy.ShouldCheckAutomatically(null, today), "A missing date should allow an automatic check.");
        Assert.True(UpdateCheckPolicy.ShouldCheckAutomatically("2026-07-10", today), "An older date should allow an automatic check.");
        Assert.True(UpdateCheckPolicy.ShouldCheckAutomatically("invalid", today), "An invalid date should allow recovery through a check.");
        Assert.False(UpdateCheckPolicy.ShouldCheckAutomatically("2026-07-11", today), "The same local day should not check twice.");
        Assert.Equal("2026-07-11", UpdateCheckPolicy.FormatDate(today), "Persisted dates should use ISO format.");
    }

    static void ManualUpdateChecksBypassTheDailyGate()
    {
        Assert.True(
            UpdateCheckPolicy.ShouldCheck(manual: true, "2026-07-11", new DateOnly(2026, 7, 11)),
            "Manual checks should bypass the daily gate.");
    }

    static void UpdateCoordinatorSkipsDevelopmentBuilds()
    {
        var service = new FakeUpdateService { IsInstalled = false };
        var settings = AppSettings.Default;
        var coordinator = new UpdateCoordinator(service, settings, _ => true, () => new DateOnly(2026, 7, 11));

        var result = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.DevelopmentBuild, result.Status, "Direct builds should not invoke installed update operations.");
        Assert.Equal(0, service.CheckCount, "Development builds should not contact the update source.");
    }

    static void DisabledUpdateServiceStaysDisabled()
    {
        var service = new DisabledUpdateService("0.1.2-dev");

        Assert.False(service.IsInstalled, "A disabled update service should never present a direct build as updater-managed.");
        Assert.Equal("0.1.2-dev", service.CurrentVersion, "A disabled service should still expose its build version.");
        Assert.True(service.CheckForUpdatesAsync(CancellationToken.None).GetAwaiter().GetResult() is null, "Disabled updates should never return a release.");
    }

    static void UpdateCoordinatorRecordsAutomaticAttemptsBeforeNetwork()
    {
        var settings = AppSettings.Default;
        var savedBeforeCheck = false;
        var service = new FakeUpdateService
        {
            OnCheck = () =>
            {
                savedBeforeCheck = settings.LastAutomaticUpdateCheckDate == "2026-07-11";
                return null;
            },
        };
        var coordinator = new UpdateCoordinator(service, settings, _ => true, () => new DateOnly(2026, 7, 11));

        var result = coordinator.CheckAsync(manual: false).GetAwaiter().GetResult();

        Assert.True(savedBeforeCheck, "The daily attempt should be persisted before awaiting the network.");
        Assert.Equal(UpdateCheckStatus.Current, result.Status, "No available release should report current.");
    }

    static void UpdateCoordinatorMapsNetworkFailures()
    {
        var service = new FakeUpdateService { Exception = new HttpRequestException("offline") };
        var coordinator = new UpdateCoordinator(service, AppSettings.Default, _ => true, () => new DateOnly(2026, 7, 11));

        var result = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.Failed, result.Status, "Network errors should map to a retryable failed status.");
    }

    static void UpdateCoordinatorLogsNetworkFailures()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new FakeUpdateService { Exception = new HttpRequestException("offline-for-test") };
        var coordinator = new UpdateCoordinator(
            service,
            AppSettings.Default,
            _ => true,
            () => new DateOnly(2026, 7, 17),
            logger: logger);

        var result = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.Failed, result.Status, "A logged network error should remain retryable.");
        var log = File.ReadAllText(paths.LogFile);
        Assert.Contains(log, "Manual update check failed", "Update logs should identify the failed operation.");
        Assert.Contains(log, "offline-for-test", "Update logs should retain the underlying exception details.");
    }

    static void UpdateCoordinatorRejectsConcurrentChecks()
    {
        var gate = new TaskCompletionSource<UpdateAvailability?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeUpdateService { PendingCheck = gate.Task };
        var coordinator = new UpdateCoordinator(service, AppSettings.Default, _ => true, () => new DateOnly(2026, 7, 11));

        var first = coordinator.CheckAsync(manual: true);
        var second = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();
        gate.SetResult(null);
        first.GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.Busy, second.Status, "A second in-flight check should return busy.");
        Assert.Equal(1, service.CheckCount, "Only one source request should run concurrently.");
    }

    static void UpdateCoordinatorAllowsOnlyOneInteractiveWorkflow()
    {
        var service = new FakeUpdateService();
        var coordinator = new UpdateCoordinator(service, AppSettings.Default, _ => true);

        using var first = coordinator.TryBeginInteractiveWorkflow();
        var second = coordinator.TryBeginInteractiveWorkflow();

        Assert.True(first is not null, "The first update interaction should acquire the workflow lease.");
        Assert.True(second is null, "A concurrent interaction must not open another update window.");

        first!.Dispose();
        using var third = coordinator.TryBeginInteractiveWorkflow();
        Assert.True(third is not null, "Disposing the first lease should allow a later interaction.");
    }

    static void TestUpdateSourceRequiresAnExplicitExistingDirectory()
    {
        using var temp = TempDirectory.Create();

        var selected = UpdateSourceOptions.Parse(["--test-update-source", temp.Path]);
        var absent = UpdateSourceOptions.Parse([]);

        Assert.Equal(System.IO.Path.GetFullPath(temp.Path), selected, "The explicit test source should resolve to an absolute directory.");
        Assert.True(absent is null, "Normal startup should not override the public GitHub update source.");
        Assert.Throws<ArgumentException>(() => UpdateSourceOptions.Parse(["--test-update-source"]));
        Assert.Throws<DirectoryNotFoundException>(() => UpdateSourceOptions.Parse(["--test-update-source", System.IO.Path.Combine(temp.Path, "missing")]));
    }

}
