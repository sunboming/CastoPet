using System.Drawing;
using CastoPet.Core;

var tests = new (string Name, Action Test)[]
{
    ("Default settings match MVP defaults", DefaultSettingsMatchMvpDefaults),
    ("Default active movement is disabled", DefaultActiveMovementIsDisabled),
    ("Default push cursor is disabled", DefaultPushCursorIsDisabled),
    ("Default input reactive mode is disabled", DefaultInputReactiveModeIsDisabled),
    ("Default theme follows the system", DefaultThemeFollowsTheSystem),
    ("Settings round trip as JSON", SettingsRoundTripAsJson),
    ("Settings round trip includes active movement", SettingsRoundTripIncludesActiveMovement),
    ("Settings round trip includes push cursor", SettingsRoundTripIncludesPushCursor),
    ("Settings round trip includes input reactive mode", SettingsRoundTripIncludesInputReactiveMode),
    ("Settings round trip includes skin manifest path", SettingsRoundTripIncludesSkinManifestPath),
    ("Settings round trip includes theme mode", SettingsRoundTripIncludesThemeMode),
    ("App paths include local crash reports", AppPathsIncludeLocalCrashReports),
    ("Settings round trip includes crash and update state", SettingsRoundTripIncludesCrashAndUpdateState),
    ("Settings clone includes crash and update state", SettingsCloneIncludesCrashAndUpdateState),
    ("Settings clone includes theme mode", SettingsCloneIncludesThemeMode),
    ("Theme mode resolves system preference", ThemeModeResolvesSystemPreference),
    ("Crash reports sanitize user paths and include exception chains", CrashReportsSanitizeUserPathsAndIncludeExceptionChains),
    ("Crash reports keep a bounded log tail", CrashReportsKeepABoundedLogTail),
    ("Crash report service writes and acknowledges reports", CrashReportServiceWritesAndAcknowledgesReports),
    ("Crash report service contains file system failures", CrashReportServiceContainsFileSystemFailures),
    ("Application registers all unhandled exception sources", ApplicationRegistersAllUnhandledExceptionSources),
    ("Crash notification is local only", CrashNotificationIsLocalOnly),
    ("Update policy checks at most once per local day", UpdatePolicyChecksAtMostOncePerLocalDay),
    ("Manual update checks bypass the daily gate", ManualUpdateChecksBypassTheDailyGate),
    ("Update coordinator skips development builds", UpdateCoordinatorSkipsDevelopmentBuilds),
    ("Update coordinator records automatic attempts before network", UpdateCoordinatorRecordsAutomaticAttemptsBeforeNetwork),
    ("Update coordinator maps network failures", UpdateCoordinatorMapsNetworkFailures),
    ("Update coordinator rejects concurrent checks", UpdateCoordinatorRejectsConcurrentChecks),
    ("Project pins semantic version and Velopack", ProjectPinsSemanticVersionAndVelopack),
    ("Velopack runs at the application entry point", VelopackRunsAtTheApplicationEntryPoint),
    ("Update source points to the public releases repository", UpdateSourcePointsToThePublicReleasesRepository),
    ("Settings window exposes crash and update actions", SettingsWindowExposesCrashAndUpdateActions),
    ("Local packaging script cannot publish artifacts", LocalPackagingScriptCannotPublishArtifacts),
    ("Pet window settings snapshot copies runtime flags", PetWindowSettingsSnapshotCopiesRuntimeFlags),
    ("Pet window settings snapshot copies input reactive mode", PetWindowSettingsSnapshotCopiesInputReactiveMode),
    ("Invalid settings file falls back to defaults", InvalidSettingsFallsBackToDefaults),
    ("Logging writes a dated log file", LoggingWritesDatedLogFile),
    ("Bottom-right placement uses work area margin", BottomRightPlacementUsesWorkAreaMargin),
    ("Startup value name is CastoPet", StartupValueNameIsCastoPet),
    ("Startup registration matches current executable path", StartupRegistrationMatchesCurrentExecutablePath),
    ("Project does not keep template MainWindow", ProjectDoesNotKeepTemplateMainWindow),
    ("Single instance rejects a second owner", SingleInstanceRejectsSecondOwner),
    ("Single instance restore signal reaches primary", SingleInstanceRestoreSignalReachesPrimary),
    ("Runtime position starts at default", RuntimePositionStartsAtDefault),
    ("Runtime position tracks drag for current run only", RuntimePositionTracksDragForCurrentRunOnly),
    ("Show restore keeps hidden position but resets visible position", ShowRestoreKeepsHiddenPositionButResetsVisiblePosition),
    ("Built-in Castorice skin defines required actions", BuiltInCastoriceSkinDefinesRequiredActions),
    ("Built-in Castorice skin uses runtime asset root", BuiltInCastoriceSkinUsesRuntimeAssetRoot),
    ("Built-in Castorice idle action preserves current frames", BuiltInCastoriceIdleActionPreservesCurrentFrames),
    ("Built-in Castorice move action preserves movement values", BuiltInCastoriceMoveActionPreservesMovementValues),
    ("Built-in Castorice blink action preserves schedule", BuiltInCastoriceBlinkActionPreservesSchedule),
    ("Built-in Castorice defines optional petting action", BuiltInCastoriceDefinesOptionalPettingAction),
    ("Built-in petting frames are packaged and clean", BuiltInPettingFramesArePackagedAndClean),
    ("Built-in Castorice expressions are ordered skin definitions", BuiltInCastoriceExpressionsAreOrderedSkinDefinitions),
    ("Pet skin manifest loads JSON resource paths", PetSkinManifestLoadsJsonResourcePaths),
    ("Pet skin manifest loads expression transition metadata", PetSkinManifestLoadsExpressionTransitionMetadata),
    ("Pet skin manifest loads file paths relative to manifest", PetSkinManifestLoadsFilePathsRelativeToManifest),
    ("Pet skin manifest requires core actions", PetSkinManifestRequiresCoreActions),
    ("Pet skin manifest writer emits loadable JSON", PetSkinManifestWriterEmitsLoadableJson),
    ("Pet skin manifest writer stores paths relative to resource root", PetSkinManifestWriterStoresPathsRelativeToResourceRoot),
    ("Pet skin manifest round trips optional petting action", PetSkinManifestRoundTripsOptionalPettingAction),
    ("Pet skin selection defaults to built-in skin", PetSkinSelectionDefaultsToBuiltInSkin),
    ("Pet skin selection loads configured manifest", PetSkinSelectionLoadsConfiguredManifest),
    ("Pet skin selection falls back when manifest fails", PetSkinSelectionFallsBackWhenManifestFails),
    ("Asset service defaults to built-in skin", AssetServiceDefaultsToBuiltInSkin),
    ("Asset service uses configured skin paths", AssetServiceUsesConfiguredSkinPaths),
    ("Asset service loads file system skin image paths", AssetServiceLoadsFileSystemSkinImagePaths),
    ("Asset service loads expression images with isolated transitions", AssetServiceLoadsExpressionImagesWithIsolatedTransitions),
    ("Asset service treats missing petting frames as optional", AssetServiceTreatsMissingPettingFramesAsOptional),
    ("Built-in idle action defines eight authored-rate frame paths", BuiltInIdleActionDefinesEightAuthoredRateFramePaths),
    ("Idle frame diagnostics read all packaged frames", IdleFrameDiagnosticsReadAllPackagedFrames),
    ("Built-in blink action defines random blink frames", BuiltInBlinkActionDefinesRandomBlinkFrames),
    ("Built-in move action defines eight distance-driven paths", BuiltInMoveActionDefinesEightDistanceDrivenPaths),
    ("Move frame paths use app resources", MoveFramePathsUseAppResources),
    ("Move speed constants stay in smooth range", MoveSpeedConstantsStayInSmoothRange),
    ("Expression wheel defines eight items", ExpressionWheelDefinesEightItems),
    ("Expression wheel paths use app resources", ExpressionWheelPathsUseAppResources),
    ("Built-in expression transition actions define shared frames", BuiltInExpressionTransitionActionsDefineSharedFrames),
    ("Expression transition paths use app resources", ExpressionTransitionPathsUseAppResources),
    ("Expression transition planner prefers specific reversible frames", ExpressionTransitionPlannerPrefersSpecificReversibleFrames),
    ("Radial wheel layout keeps generic two-ring geometry", RadialWheelLayoutKeepsGenericTwoRingGeometry),
    ("Radial wheel style keeps readable ring hierarchy", RadialWheelStyleKeepsReadableRingHierarchy),
    ("Pointer gestures classify left click and drag", PointerGesturesClassifyLeftClickAndDrag),
    ("Pointer gestures classify right click movement and hold", PointerGesturesClassifyRightClickMovementAndHold),
    ("Pointer gestures cancel conflicts and commit once", PointerGesturesCancelConflictsAndCommitOnce),
    ("Wheel catalog preserves ordered action references", WheelCatalogPreservesOrderedActionReferences),
    ("Wheel catalog exposes disabled empty shortcut content", WheelCatalogExposesDisabledEmptyShortcutContent),
    ("Wheel catalog service refreshes successful shortcut mutations", WheelCatalogServiceRefreshesSuccessfulShortcutMutations),
    ("Wheel catalog service unsubscribes when disposed", WheelCatalogServiceUnsubscribesWhenDisposed),
    ("Application composes one shared shortcut wheel graph", ApplicationComposesOneSharedShortcutWheelGraph),
    ("Pet window follows live wheel catalog snapshots", PetWindowFollowsLiveWheelCatalogSnapshots),
    ("Radial wheel selector distinguishes all pointer regions", RadialWheelSelectorDistinguishesAllPointerRegions),
    ("Radial wheel controller honors category dwell", RadialWheelControllerHonorsCategoryDwell),
    ("Radial wheel tolerates slight outer overshoot", RadialWheelToleratesSlightOuterOvershoot),
    ("Radial wheel controller resets and collapses state", RadialWheelControllerResetsAndCollapsesState),
    ("Radial wheel controller paginates without persisting controls", RadialWheelControllerPaginatesWithoutPersistingControls),
    ("Shortcut service loads empty state and round trips", ShortcutServiceLoadsEmptyStateAndRoundTrips),
    ("Shortcut service normalizes duplicate identities", ShortcutServiceNormalizesDuplicateIdentities),
    ("Shortcut service appends candidates with contiguous ordering", ShortcutServiceAppendsCandidatesWithContiguousOrdering),
    ("Shortcut service mutates ordered entries", ShortcutServiceMutatesOrderedEntries),
    ("Shortcut service updates program launch options safely", ShortcutServiceUpdatesProgramLaunchOptionsSafely),
    ("Shortcut service enforces its entry limit", ShortcutServiceEnforcesEntryLimit),
    ("Shortcut service recovers malformed storage", ShortcutServiceRecoversMalformedStorage),
    ("Shortcut service isolates malformed entries", ShortcutServiceIsolatesMalformedEntries),
    ("Shortcut service notifies only after persisted mutations", ShortcutServiceNotifiesOnlyAfterPersistedMutations),
    ("Shortcut drop handler classifies existing file system items", ShortcutDropHandlerClassifiesExistingFileSystemItems),
    ("Shortcut drop handler rejects executable scripts", ShortcutDropHandlerRejectsExecutableScripts),
    ("Shortcut drop handler accepts safe web targets", ShortcutDropHandlerAcceptsSafeWebTargets),
    ("Shortcut drop handler rejects missing and unsafe inputs", ShortcutDropHandlerRejectsMissingAndUnsafeInputs),
    ("Shortcut drop handler aggregates mixed batch duplicates", ShortcutDropHandlerAggregatesMixedBatchDuplicates),
    ("Shortcut drop handler reports shortcut limit failures", ShortcutDropHandlerReportsShortcutLimitFailures),
    ("Shortcut launcher creates structured shell start info", ShortcutLauncherCreatesStructuredShellStartInfo),
    ("Shortcut launcher accepts every supported target type", ShortcutLauncherAcceptsEverySupportedTargetType),
    ("Shortcut launcher rejects missing and malformed definitions", ShortcutLauncherRejectsMissingAndMalformedDefinitions),
    ("Shortcut launcher rejects tampered executable file definitions", ShortcutLauncherRejectsTamperedExecutableFileDefinitions),
    ("Shortcut launcher contains and logs start failures", ShortcutLauncherContainsAndLogsStartFailures),
    ("Pet window defines two-level radial overlay and drop surface", PetWindowDefinesTwoLevelRadialOverlayAndDropSurface),
    ("Pet window consumes centralized radial wheel styling", PetWindowConsumesCentralizedRadialWheelStyling),
    ("Pet window routes classified pointer gestures", PetWindowRoutesClassifiedPointerGestures),
    ("Pet window defines hold feedback and petting playback", PetWindowDefinesHoldFeedbackAndPettingPlayback),
    ("Pet window routes generic radial actions", PetWindowRoutesGenericRadialActions),
    ("Pet window extracts neutral shortcut drop data", PetWindowExtractsNeutralShortcutDropData),
    ("Pet window retires expression-only wheel integration", PetWindowRetiresExpressionOnlyWheelIntegration),
    ("Setting catalog defines every boolean setting once", SettingCatalogDefinesEveryBooleanSettingOnce),
    ("Setting catalog exposes only common direct menu settings", SettingCatalogExposesOnlyCommonDirectMenuSettings),
    ("Setting catalog reads shared settings live", SettingCatalogReadsSharedSettingsLive),
    ("Settings window service reuses the open window", SettingsWindowServiceReusesTheOpenWindow),
    ("Settings window service releases a closed window", SettingsWindowServiceReleasesAClosedWindow),
    ("Settings window defines the approved visual structure", SettingsWindowDefinesTheApprovedVisualStructure),
    ("Settings window exposes shortcut launcher management", SettingsWindowExposesShortcutLauncherManagement),
    ("Settings window shares shortcut services and live updates", SettingsWindowSharesShortcutServicesAndLiveUpdates),
    ("Direct menus expose the settings command", DirectMenusExposeTheSettingsCommand),
    ("Input keyboard layout maps common keys", InputKeyboardLayoutMapsCommonKeys),
    ("Input keyboard layout exposes drawable keys", InputKeyboardLayoutExposesDrawableKeys),
    ("Input keyboard layout exposes key labels", InputKeyboardLayoutExposesKeyLabels),
    ("Input reactive state expires highlights", InputReactiveStateExpiresHighlights),
    ("Windows input hook normalizes common keys", WindowsInputHookNormalizesCommonKeys),
    ("Input reactive mode suppresses passive animation", InputReactiveModeSuppressesPassiveAnimation),
    ("Movement planner clamps targets to work area", MovementPlannerClampsTargetsToWorkArea),
    ("Movement planner approaches mouse with cursor offset", MovementPlannerApproachesMouseWithCursorOffset),
    ("Movement planner eases toward target", MovementPlannerEasesTowardTarget),
    ("Movement planner detects close targets", MovementPlannerDetectsCloseTargets),
    ("Movement planner detects mouse approach rest position", MovementPlannerDetectsMouseApproachRestPosition),
    ("Cursor nudge planner nudges nearby cursor", CursorNudgePlannerNudgesNearbyCursor),
    ("Cursor nudge planner ignores distant cursor", CursorNudgePlannerIgnoresDistantCursor),
    ("Cursor nudge planner clamps to work area", CursorNudgePlannerClampsToWorkArea),
    ("Cursor nudge planner detects manual movement cooldown", CursorNudgePlannerDetectsManualMovementCooldown),
    ("Cursor nudge planner blocks while mouse button is pressed", CursorNudgePlannerBlocksWhileMouseButtonIsPressed),
    ("Cursor nudge planner limits continuous push duration", CursorNudgePlannerLimitsContinuousPushDuration),
    ("Pet animation timings are responsive", PetAnimationTimingsAreResponsive),
    ("Idle breathing values are neutral during stabilization", IdleBreathingValuesAreNeutralDuringStabilization),
    ("Character stationary animations are enabled", CharacterStationaryAnimationsAreEnabled),
    ("Character assets decode at pet display width", CharacterAssetsDecodeAtPetDisplayWidth),
    ("Asset diagnostics include group and resource path", AssetDiagnosticsIncludeGroupAndResourcePath),
    ("Input reactive asset path uses app resource", InputReactiveAssetPathUsesAppResource),
    ("Input reactive asset is packaged", InputReactiveAssetIsPackaged),
    ("Packaged character assets are display sized", PackagedCharacterAssetsAreDisplaySized),
    ("Packaged expression transitions have complete source and runtime endpoints", PackagedExpressionTransitionsHaveCompleteSourceAndRuntimeEndpoints),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failures;

static void DefaultSettingsMatchMvpDefaults()
{
    var settings = AppSettings.Default;
    Assert.True(settings.Topmost, "Topmost should default to true.");
    Assert.False(settings.ClickThrough, "ClickThrough should default to false.");
    Assert.False(settings.ShowInTaskbar, "ShowInTaskbar should default to false.");
    Assert.False(settings.StartWithWindows, "StartWithWindows should default to false.");
    Assert.False(settings.ActiveMovement, "ActiveMovement should default to false.");
    Assert.False(settings.PushCursor, "PushCursor should default to false.");
}

static void DefaultActiveMovementIsDisabled()
{
    var settings = AppSettings.Default;

    Assert.False(settings.ActiveMovement, "Active movement should default to false.");
}

static void DefaultPushCursorIsDisabled()
{
    var settings = AppSettings.Default;

    Assert.False(settings.PushCursor, "Push cursor should default to false.");
}

static void DefaultInputReactiveModeIsDisabled()
{
    var settings = AppSettings.Default;

    Assert.False(settings.InputReactiveMode, "Input reactive mode should default to false.");
}

static void DefaultThemeFollowsTheSystem()
{
    Assert.Equal(AppThemeMode.System, AppSettings.Default.ThemeMode, "Existing users should follow the Windows app theme by default.");
}

static void SettingsRoundTripAsJson()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        StartWithWindows = true,
        ActiveMovement = true,
        PushCursor = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.False(loaded.Topmost, "Topmost should round trip.");
    Assert.True(loaded.ClickThrough, "ClickThrough should round trip.");
    Assert.True(loaded.ShowInTaskbar, "ShowInTaskbar should round trip.");
    Assert.True(loaded.StartWithWindows, "StartWithWindows should round trip.");
    Assert.True(loaded.ActiveMovement, "ActiveMovement should round trip.");
    Assert.True(loaded.PushCursor, "PushCursor should round trip.");
}

static void SettingsRoundTripIncludesActiveMovement()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        StartWithWindows = true,
        ActiveMovement = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.True(loaded.ActiveMovement, "ActiveMovement should round trip.");
}

static void SettingsRoundTripIncludesPushCursor()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        PushCursor = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.True(loaded.PushCursor, "PushCursor should round trip.");
}

static void SettingsRoundTripIncludesInputReactiveMode()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        InputReactiveMode = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.True(loaded.InputReactiveMode, "InputReactiveMode should round trip.");
}

static void SettingsRoundTripIncludesSkinManifestPath()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        SkinManifestPath = @"D:\Skins\Custom\skin.json",
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.Equal(@"D:\Skins\Custom\skin.json", loaded.SkinManifestPath, "Skin manifest path should round trip.");
}

static void SettingsRoundTripIncludesThemeMode()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new SettingsService(paths, new LoggingService(paths));
    var settings = new AppSettings { ThemeMode = AppThemeMode.Dark };

    service.Save(settings);
    var loaded = service.Load();

    Assert.Equal(AppThemeMode.Dark, loaded.ThemeMode, "Theme mode should round trip.");
}

static void AppPathsIncludeLocalCrashReports()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);

    Assert.Equal(
        System.IO.Path.Combine(temp.Path, "Crashes"),
        paths.CrashesDirectory,
        "Crash reports should live beside settings and logs in the application data directory.");
}

static void SettingsRoundTripIncludesCrashAndUpdateState()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new SettingsService(paths, new LoggingService(paths));
    var settings = new AppSettings
    {
        LastAcknowledgedCrashId = "crash-20260711-120000-test",
        LastAutomaticUpdateCheckDate = "2026-07-11",
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.Equal(settings.LastAcknowledgedCrashId, loaded.LastAcknowledgedCrashId, "Crash acknowledgement should round trip.");
    Assert.Equal(settings.LastAutomaticUpdateCheckDate, loaded.LastAutomaticUpdateCheckDate, "Update check date should round trip.");
}

static void SettingsCloneIncludesCrashAndUpdateState()
{
    var settings = new AppSettings
    {
        LastAcknowledgedCrashId = "crash-id",
        LastAutomaticUpdateCheckDate = "2026-07-11",
    };

    var clone = settings.Clone();

    Assert.Equal(settings.LastAcknowledgedCrashId, clone.LastAcknowledgedCrashId, "Clone should retain crash acknowledgement.");
    Assert.Equal(settings.LastAutomaticUpdateCheckDate, clone.LastAutomaticUpdateCheckDate, "Clone should retain update check date.");
}

static void SettingsCloneIncludesThemeMode()
{
    var settings = new AppSettings { ThemeMode = AppThemeMode.Light };

    Assert.Equal(AppThemeMode.Light, settings.Clone().ThemeMode, "Clone should retain the selected theme mode.");
}

static void ThemeModeResolvesSystemPreference()
{
    Assert.Equal(AppThemeMode.Light, ThemeModeResolver.Resolve(AppThemeMode.Light, systemUsesDark: true), "Explicit light mode should ignore the system theme.");
    Assert.Equal(AppThemeMode.Dark, ThemeModeResolver.Resolve(AppThemeMode.Dark, systemUsesDark: false), "Explicit dark mode should ignore the system theme.");
    Assert.Equal(AppThemeMode.Dark, ThemeModeResolver.Resolve(AppThemeMode.System, systemUsesDark: true), "System mode should resolve to dark when Windows uses dark apps.");
    Assert.Equal(AppThemeMode.Light, ThemeModeResolver.Resolve(AppThemeMode.System, systemUsesDark: false), "System mode should resolve to light when Windows uses light apps.");
}

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
    var service = new CrashReportService(paths, new LoggingService(paths));

    var written = service.TryWriteReport(new InvalidOperationException("test crash"), out var report);

    Assert.True(written, "Crash report write should succeed in a writable data directory.");
    Assert.True(report is not null, "A successful write should return report metadata.");
    Assert.True(File.Exists(report!.Path), "Crash report metadata should point to the written file.");
    Assert.Equal(report.Id, System.IO.Path.GetFileNameWithoutExtension(report.Path), "Report ID should match its filename.");
    Assert.Equal(report.Id, service.GetLatestUnacknowledged(null)?.Id, "An unacknowledged report should be discovered.");
    Assert.True(service.GetLatestUnacknowledged(report.Id) is null, "Acknowledged reports should not be returned again.");
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

static void ApplicationRegistersAllUnhandledExceptionSources()
{
    var workspace = FindWorkspaceRoot();
    var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

    Assert.Contains(appSource, "DispatcherUnhandledException", "WPF dispatcher exceptions should be recorded.");
    Assert.Contains(appSource, "AppDomain.CurrentDomain.UnhandledException", "Non-UI fatal exceptions should be recorded.");
    Assert.Contains(appSource, "TaskScheduler.UnobservedTaskException", "Unobserved task exceptions should be recorded.");
}

static void CrashNotificationIsLocalOnly()
{
    var workspace = FindWorkspaceRoot();
    var xamlPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "CrashNotificationWindow.xaml");
    var xaml = File.ReadAllText(xamlPath);

    Assert.Contains(xaml, "打开日志目录", "Crash notification should provide local report access.");
    Assert.Contains(xaml, "忽略", "Crash notification should support acknowledgement.");
    Assert.False(xaml.Contains("上传", StringComparison.Ordinal), "Crash notification should not imply network upload.");
}

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

static void ProjectPinsSemanticVersionAndVelopack()
{
    var workspace = FindWorkspaceRoot();
    var project = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj"));

    Assert.Contains(project, "<Version>0.1.0</Version>", "The application should have an explicit semantic version.");
    Assert.Contains(project, "<PackageReference Include=\"Velopack\" Version=\"1.2.0\"", "Velopack should be pinned to the verified stable version.");
}

static void VelopackRunsAtTheApplicationEntryPoint()
{
    var workspace = FindWorkspaceRoot();
    var program = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Program.cs"));
    var app = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

    Assert.Contains(program, "VelopackApp.Build().Run();", "Velopack hooks should run at the beginning of Main.");
    Assert.Contains(program, "static void Main", "The application should expose an explicit entry point.");
    Assert.False(app.Contains("VelopackApp.Build().Run();", StringComparison.Ordinal), "Velopack hooks should not wait until the App constructor.");
}

static void UpdateSourcePointsToThePublicReleasesRepository()
{
    Assert.Equal(
        "https://github.com/sunboming/CastoPet-Releases",
        VelopackUpdateService.RepositoryUrl,
        "Installed builds should use the public releases repository without a client token.");
}

static void SettingsWindowExposesCrashAndUpdateActions()
{
    var workspace = FindWorkspaceRoot();
    var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "SettingsWindow.xaml"));

    Assert.Contains(xaml, "OpenCrashReportsButton", "Settings should expose local crash reports.");
    Assert.Contains(xaml, "CheckForUpdatesButton", "Settings should expose manual update checks.");
    Assert.Contains(xaml, "UpdateStatusText", "Settings should display update status.");
    Assert.Contains(xaml, "CurrentVersionText", "Settings should display the current version.");
}

static void LocalPackagingScriptCannotPublishArtifacts()
{
    var workspace = FindWorkspaceRoot();
    var script = File.ReadAllText(System.IO.Path.Combine(workspace, "tools", "package-local.ps1"));
    var gitignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

    Assert.Contains(script, "dotnet publish", "Local packaging should publish a self-contained application first.");
    Assert.Contains(script, "--self-contained", "Local packaging should not require a preinstalled runtime.");
    Assert.Contains(script, "win-x64", "The first installer should target Windows x64.");
    Assert.Contains(script, "vpk pack", "Local packaging should create a Velopack installer.");
    Assert.Contains(script, "--packId CastoPet.App", "Installer files must not share the CastoPet user-data directory.");
    Assert.False(script.Contains("vpk upload", StringComparison.OrdinalIgnoreCase), "Local packaging must not upload packages.");
    Assert.False(script.Contains("gh release", StringComparison.OrdinalIgnoreCase), "Local packaging must not create GitHub Releases.");
    Assert.Contains(gitignore, "artifacts/local-package/", "Generated local packages should stay outside version control.");
}

static void PetWindowSettingsSnapshotCopiesRuntimeFlags()
{
    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        ActiveMovement = true,
        PushCursor = true,
    };

    var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);

    Assert.False(snapshot.Topmost, "Topmost should be copied for immediate window application.");
    Assert.True(snapshot.ClickThrough, "Click-through should be copied for Win32 window style application.");
    Assert.True(snapshot.ShowInTaskbar, "Taskbar visibility should be copied for window application.");
    Assert.True(snapshot.ActiveMovement, "Active movement should be copied for movement runtime state.");
    Assert.True(snapshot.PushCursor, "Push cursor should be copied for movement runtime state.");
}

static void PetWindowSettingsSnapshotCopiesInputReactiveMode()
{
    var settings = new AppSettings
    {
        InputReactiveMode = true,
    };

    var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);

    Assert.True(snapshot.InputReactiveMode, "Input reactive mode should be copied for window runtime state.");
}

static void InvalidSettingsFallsBackToDefaults()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    Directory.CreateDirectory(paths.DataDirectory);
    File.WriteAllText(paths.SettingsFile, "{not valid json");

    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);
    var loaded = service.Load();

    Assert.True(loaded.Topmost, "Invalid settings should return defaults.");
    Assert.False(loaded.ClickThrough, "Invalid settings should return defaults.");
    Assert.False(loaded.ActiveMovement, "Invalid settings should return defaults.");
    Assert.False(loaded.PushCursor, "Invalid settings should return defaults.");
    Assert.True(File.Exists(paths.LogFile), "Invalid settings should be logged.");
}

static void LoggingWritesDatedLogFile()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);

    logger.Info("hello");

    Assert.True(File.Exists(paths.LogFile), "Log file should exist.");
    var text = File.ReadAllText(paths.LogFile);
    Assert.Contains(text, "hello", "Log file should include message.");
}

static void BottomRightPlacementUsesWorkAreaMargin()
{
    var bounds = WindowPlacementService.CalculateBottomRight(
        workAreaLeft: 0,
        workAreaTop: 0,
        workAreaWidth: 1920,
        workAreaHeight: 1080,
        windowWidth: 320,
        windowHeight: 420,
        margin: 24);

    Assert.Equal(1576, (int)bounds.Left, "Left should place window near the right edge.");
    Assert.Equal(636, (int)bounds.Top, "Top should place window near the bottom edge.");
}

static void StartupValueNameIsCastoPet()
{
    Assert.Equal("CastoPet", StartupService.ValueName, "Startup registry value should use app name.");
}

static void StartupRegistrationMatchesCurrentExecutablePath()
{
    Assert.True(
        StartupService.MatchesExecutablePath(
            "\"C:\\Apps\\CastoPet\\CastoPet.exe\"",
            "C:\\Apps\\CastoPet\\CastoPet.exe"),
        "Quoted registry path should match the executable path.");
    Assert.True(
        StartupService.MatchesExecutablePath(
            "C:\\Apps\\CastoPet\\CastoPet.exe",
            "C:\\Apps\\CastoPet\\CastoPet.exe"),
        "Unquoted registry path should match the executable path.");
    Assert.False(
        StartupService.MatchesExecutablePath(
            "\"C:\\Old\\CastoPet.exe\"",
            "C:\\Apps\\CastoPet\\CastoPet.exe"),
        "Different registry path should not count as enabled for this executable.");
}

static void ProjectDoesNotKeepTemplateMainWindow()
{
    var workspace = FindWorkspaceRoot();

    Assert.False(
        File.Exists(System.IO.Path.Combine(workspace, "src", "CastoPet", "MainWindow.xaml")),
        "Template MainWindow.xaml should not be kept in the tray-only pet app.");
    Assert.False(
        File.Exists(System.IO.Path.Combine(workspace, "src", "CastoPet", "MainWindow.xaml.cs")),
        "Template MainWindow.xaml.cs should not be kept in the tray-only pet app.");
}

static void SingleInstanceRejectsSecondOwner()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var scope = "CastoPet.Tests." + Guid.NewGuid().ToString("N");

    using var first = new SingleInstanceService(logger, scope);
    using var second = new SingleInstanceService(logger, scope);

    Assert.True(first.IsPrimaryInstance, "First service should own the instance mutex.");
    Assert.False(second.IsPrimaryInstance, "Second service should not own the same instance mutex.");
}

static void SingleInstanceRestoreSignalReachesPrimary()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var scope = "CastoPet.Tests." + Guid.NewGuid().ToString("N");
    using var first = new SingleInstanceService(logger, scope);
    using var second = new SingleInstanceService(logger, scope);
    using var restored = new ManualResetEventSlim(false);

    first.StartRestoreServer(() => restored.Set());

    var signaled = second.SignalRestoreAsync().GetAwaiter().GetResult();

    Assert.True(signaled, "Second instance should signal primary without pipe errors.");
    Assert.True(restored.Wait(TimeSpan.FromSeconds(2)), "Primary should receive restore signal.");
}

static void RuntimePositionStartsAtDefault()
{
    var state = new PetRuntimeState();

    Assert.False(state.HasRuntimePosition, "New runtime state should not have a dragged position.");
}

static void RuntimePositionTracksDragForCurrentRunOnly()
{
    var state = new PetRuntimeState();

    state.SetRuntimePosition(120, 240);

    Assert.True(state.HasRuntimePosition, "Dragged position should be tracked during this run.");
    Assert.Equal(120d, state.Left, "Dragged left should be stored.");
    Assert.Equal(240d, state.Top, "Dragged top should be stored.");
}

static void ShowRestoreKeepsHiddenPositionButResetsVisiblePosition()
{
    var state = new PetRuntimeState();
    state.SetRuntimePosition(120, 240);

    var hiddenAction = state.GetShowRestoreAction(isVisible: false);
    var visibleAction = state.GetShowRestoreAction(isVisible: true);

    Assert.Equal(PetShowRestoreAction.ShowAtRuntimePosition, hiddenAction, "Hidden pet should reappear at current runtime position.");
    Assert.Equal(PetShowRestoreAction.RestoreDefaultPosition, visibleAction, "Visible pet should restore to default position.");
    Assert.False(state.HasRuntimePosition, "Restoring visible pet to default should clear runtime position.");
}

static void BuiltInCastoriceSkinDefinesRequiredActions()
{
    var skin = BuiltInPetSkins.Castorice;

    Assert.Equal("castorice", skin.Id, "Built-in skin id should be stable.");
    Assert.Equal("Castorice", skin.DisplayName, "Built-in skin display name should be stable.");
    Assert.Equal("Assets/Runtime/Castorice/Castorice.png", skin.DefaultCharacterPath, "Default character path should use runtime root.");
    Assert.Equal("Assets/Runtime/Castorice/States/Castorice.Dragging.png", skin.DraggingCharacterPath, "Dragging path should use runtime root.");
    Assert.Equal("Assets/Runtime/Castorice/States/InputReactive/Castorice.InputReactive.Base.png", skin.InputReactiveBasePath, "Input reactive path should use runtime root.");
    Assert.True(skin.TryGetAction(PetActionKind.Idle, out _), "Castorice should define idle.");
    Assert.True(skin.TryGetAction(PetActionKind.Move, out _), "Castorice should define move.");
    Assert.True(skin.TryGetAction(PetActionKind.Blink, out _), "Castorice should define blink.");
    Assert.True(skin.TryGetAction(PetActionKind.ExpressionTransitionIn, out _), "Castorice should define transition in.");
    Assert.True(skin.TryGetAction(PetActionKind.ExpressionTransitionOut, out _), "Castorice should define transition out.");
}

static void BuiltInCastoriceSkinUsesRuntimeAssetRoot()
{
    var skin = BuiltInPetSkins.Castorice;
    var paths = new List<string>
    {
        skin.DefaultCharacterPath,
        skin.DraggingCharacterPath,
        skin.InputReactiveBasePath,
    };
    paths.AddRange(skin.Actions.SelectMany(action => action.FramePaths));
    paths.AddRange(skin.Expressions.Select(expression => expression.ResourcePath));

    Assert.True(paths.All(path => path.StartsWith("Assets/Runtime/Castorice/", StringComparison.Ordinal)), "Built-in runtime paths should live under Assets/Runtime/Castorice.");
}

static void BuiltInCastoriceIdleActionPreservesCurrentFrames()
{
    var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

    Assert.Equal(8, idle.FramePaths.Count, "Idle should keep eight frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(125), idle.FrameInterval, "Idle should play at the authored 8 FPS rate.");
    Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.00.png", idle.FramePaths[0], "First idle frame path should stay compatible.");
    Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.07.png", idle.FramePaths[^1], "Last idle frame path should stay compatible.");
}

static void BuiltInCastoriceMoveActionPreservesMovementValues()
{
    var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

    Assert.Equal(8, move.FramePaths.Count, "Move should keep eight frames.");
    Assert.Equal(10d, move.DistancePerFrame, "Move distance per frame should stay compatible.");
    Assert.Equal(90d, move.BaseSpeedPixelsPerSecond, "Move base speed should stay compatible.");
    Assert.Equal(80d, move.MinSpeedPixelsPerSecond, "Move min speed should stay compatible.");
    Assert.Equal(105d, move.MaxSpeedPixelsPerSecond, "Move max speed should stay compatible.");
}

static void BuiltInCastoriceBlinkActionPreservesSchedule()
{
    var blink = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Blink);

    Assert.Equal(3, blink.FramePaths.Count, "Blink should keep three frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), blink.FrameInterval, "Blink frame interval should stay compatible.");
    Assert.Equal(TimeSpan.FromSeconds(3), blink.MinScheduleDelay, "Blink min schedule should stay compatible.");
    Assert.Equal(TimeSpan.FromSeconds(7), blink.MaxScheduleDelay, "Blink max schedule should stay compatible.");
}

static void BuiltInCastoriceDefinesOptionalPettingAction()
{
    Assert.True(BuiltInPetSkins.Castorice.TryGetAction(PetActionKind.Petting, out var petting), "Castorice should define petting without making it a required skin action.");
    Assert.Equal(8, petting.FramePaths.Count, "Petting should define eight authored frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(80), petting.FrameInterval, "Petting should play once at 12.5 FPS.");
    Assert.Equal("Assets/Runtime/Castorice/States/Petting/Castorice.Petting.00.png", petting.FramePaths[0], "Petting paths should use the runtime convention.");
    Assert.Equal("Assets/Runtime/Castorice/States/Petting/Castorice.Petting.07.png", petting.FramePaths[^1], "Petting should end on frame seven.");
}

static void BuiltInPettingFramesArePackagedAndClean()
{
    var workspace = FindWorkspaceRoot();
    var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
    var runtimeRoot = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "Petting");
    var frames = Enumerable.Range(0, 8)
        .Select(index => System.IO.Path.Combine(runtimeRoot, $"Castorice.Petting.{index:00}.png"))
        .ToArray();

    Assert.True(frames.All(File.Exists), "Built-in petting should include eight consecutive runtime PNGs.");
    foreach (var frame in frames)
    {
        using var bitmap = new Bitmap(frame);
        Assert.Equal(320, bitmap.Width, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels wide.");
        Assert.Equal(320, bitmap.Height, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels high.");
        Assert.True(bitmap.GetPixel(0, 0).A == 0, $"{System.IO.Path.GetFileName(frame)} should keep a transparent background.");

        var greenFringePixels = 0;
        for (var y = 0; y < bitmap.Height; y += 2)
        {
            for (var x = 0; x < bitmap.Width; x += 2)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 8 && pixel.G > pixel.R + 40 && pixel.G > pixel.B + 40)
                {
                    greenFringePixels++;
                }
            }
        }

        Assert.True(greenFringePixels <= 2, $"{System.IO.Path.GetFileName(frame)} should not contain a visible cluster of green-key fringe pixels.");
    }

    var idle = File.ReadAllBytes(System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "Idle", "Castorice.Idle.00.png"));
    Assert.True(idle.SequenceEqual(File.ReadAllBytes(frames[0])), "Petting frame 00 should use the idle baseline.");
    Assert.True(idle.SequenceEqual(File.ReadAllBytes(frames[^1])), "Petting frame 07 should return exactly to the idle baseline.");

    var projectText = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
    Assert.Contains(projectText, @"Assets\Runtime\Castorice\States\Petting\*.png", "Petting frames should be packaged as WPF resources.");

    using var temp = TempDirectory.Create();
    var petting = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Petting) with
    {
        FramePaths = frames,
    };
    var skin = BuiltInPetSkins.Castorice with
    {
        Actions = BuiltInPetSkins.Castorice.Actions
            .Where(action => action.Kind != PetActionKind.Petting)
            .Append(petting)
            .ToArray(),
    };
    var service = new AssetService(new LoggingService(new AppPaths(temp.Path)), skin);
    Assert.Equal(8, service.LoadPettingFrames().Count, "AssetService should load the complete built-in petting sequence.");
}

static void BuiltInCastoriceExpressionsAreOrderedSkinDefinitions()
{
    var expressions = BuiltInPetSkins.Castorice.Expressions;

    Assert.Equal(8, expressions.Count, "Castorice should keep eight expression wheel items.");
    Assert.Equal("happy", expressions[0].Id, "First expression id should be stable.");
    Assert.Equal("Happy", expressions[0].Label, "First expression label should be stable.");
    Assert.Equal("Assets/Runtime/Castorice/Expressions/Castorice.Expression.Happy.png", expressions[0].ResourcePath, "First expression path should stay compatible.");
    Assert.Equal(6, expressions[0].TransitionFramePaths?.Count, "Each built-in expression should define six transition frames.");
    Assert.Equal("Assets/Runtime/Castorice/Expressions/Happy/Transition/Castorice.Expression.Happy.Transition.00.png", expressions[0].TransitionFramePaths?[0], "First expression transition frame should use the runtime convention.");
    Assert.Equal(TimeSpan.FromMilliseconds(1000d / 15d), expressions[0].TransitionFrameInterval, "Expression transitions should play at 15 FPS.");
    Assert.Equal("crying", expressions[^1].Id, "Last expression id should be stable.");
    Assert.Equal("Crying", expressions[^1].Label, "Last expression label should be stable.");
}

static void PetSkinManifestLoadsJsonResourcePaths()
{
    var skin = PetSkinManifestLoader.LoadFromJson("""
        {
          "schemaVersion": 1,
          "id": "custom",
          "displayName": "Custom Skin",
          "resourceRoot": "Skins/Custom",
          "defaultCharacter": "Default.png",
          "draggingCharacter": "States/Dragging.png",
          "inputReactiveBase": "Input/Base.png",
          "actions": [
            {
              "id": "idle",
              "kind": "idle",
              "frameIntervalMs": 200,
              "frames": ["Idle/00.png", "Idle/01.png"]
            },
            {
              "id": "move",
              "kind": "move",
              "distancePerFrame": 10,
              "baseSpeedPixelsPerSecond": 90,
              "minSpeedPixelsPerSecond": 80,
              "maxSpeedPixelsPerSecond": 105,
              "frames": ["Move/00.png"]
            },
            {
              "id": "blink",
              "kind": "blink",
              "frameIntervalMs": 90,
              "minScheduleDelayMs": 3000,
              "maxScheduleDelayMs": 7000,
              "frames": ["Blink/00.png"]
            }
          ],
          "expressions": {
            "Happy": "Expressions/Happy.png"
          }
        }
        """);

    Assert.Equal("custom", skin.Id, "Manifest id should load.");
    Assert.Equal("Custom Skin", skin.DisplayName, "Manifest display name should load.");
    Assert.Equal("Skins/Custom", skin.ResourceRoot, "Manifest resource root should load.");
    Assert.Equal("Skins/Custom/Default.png", skin.DefaultCharacterPath, "JSON manifest paths should resolve under resource root.");
    Assert.Equal("Skins/Custom/States/Dragging.png", skin.DraggingCharacterPath, "Optional dragging path should resolve under resource root.");
    Assert.Equal("Skins/Custom/Input/Base.png", skin.InputReactiveBasePath, "Optional input base path should resolve under resource root.");
    Assert.Equal("Skins/Custom/Idle/00.png", skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "Action frames should resolve under resource root.");
    Assert.Equal(TimeSpan.FromMilliseconds(200), skin.GetRequiredAction(PetActionKind.Idle).FrameInterval, "Action frame interval should load.");
    Assert.Equal(10d, skin.GetRequiredAction(PetActionKind.Move).DistancePerFrame, "Move distance should load.");
    Assert.Equal(TimeSpan.FromMilliseconds(3000), skin.GetRequiredAction(PetActionKind.Blink).MinScheduleDelay, "Blink min schedule should load.");
    Assert.Equal("Happy", skin.Expressions[0].Label, "Expression labels should load.");
    Assert.Equal("Skins/Custom/Expressions/Happy.png", skin.Expressions[0].ResourcePath, "Expression paths should resolve under resource root.");
}

static void PetSkinManifestLoadsExpressionTransitionMetadata()
{
    var skin = PetSkinManifestLoader.LoadFromJson("""
        {
          "schemaVersion": 2,
          "id": "animated",
          "displayName": "Animated Skin",
          "resourceRoot": "Skins/Animated",
          "defaultCharacter": "Default.png",
          "actions": [
            { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
            { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
            { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
          ],
          "expressions": {
            "Happy": {
              "image": "Expressions/Happy.png",
              "transitionFrames": [
                "Expressions/Happy/Transition/00.png",
                "Expressions/Happy/Transition/01.png"
              ],
              "transitionFrameIntervalMs": 66.6667
            }
          }
        }
        """);

    var expression = skin.Expressions.Single();
    Assert.Equal("Skins/Animated/Expressions/Happy.png", expression.ResourcePath, "Expression image should resolve under resource root.");
    Assert.Equal(2, expression.TransitionFramePaths?.Count, "Expression transition frames should load.");
    Assert.Equal("Skins/Animated/Expressions/Happy/Transition/00.png", expression.TransitionFramePaths?[0], "Transition frame path should resolve under resource root.");
    Assert.Equal(TimeSpan.FromMilliseconds(66.6667), expression.TransitionFrameInterval, "Expression transition interval should load.");
}

static void PetSkinManifestLoadsFilePathsRelativeToManifest()
{
    using var temp = TempDirectory.Create();
    var manifestDirectory = System.IO.Path.Combine(temp.Path, "Pack");
    Directory.CreateDirectory(manifestDirectory);
    var manifestPath = System.IO.Path.Combine(manifestDirectory, "skin.json");
    File.WriteAllText(manifestPath, """
        {
          "schemaVersion": 1,
          "id": "file-skin",
          "displayName": "File Skin",
          "resourceRoot": "Resources",
          "defaultCharacter": "Default.png",
          "actions": [
            { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
            { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
            { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
          ]
        }
        """);

    var skin = PetSkinManifestLoader.LoadFromFile(manifestPath);
    var expectedRoot = System.IO.Path.Combine(manifestDirectory, "Resources");

    Assert.Equal(System.IO.Path.Combine(expectedRoot, "Default.png"), skin.DefaultCharacterPath, "File manifest paths should resolve relative to manifest directory.");
    Assert.Equal(System.IO.Path.Combine(expectedRoot, "Idle", "00.png"), skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "File action paths should resolve relative to manifest directory.");
}

static void PetSkinManifestRequiresCoreActions()
{
    var ex = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
        {
          "schemaVersion": 1,
          "id": "broken",
          "displayName": "Broken",
          "defaultCharacter": "Default.png",
          "actions": [
            { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] }
          ]
        }
        """));

    Assert.Contains(ex.Message, "Move", "Manifest validation should identify missing move action.");
}

static void PetSkinManifestWriterEmitsLoadableJson()
{
    using var temp = TempDirectory.Create();
    var manifestPath = System.IO.Path.Combine(temp.Path, "skin.json");

    PetSkinManifestWriter.WriteToFile(manifestPath, BuiltInPetSkins.Castorice);
    var skin = PetSkinManifestLoader.LoadFromJson(File.ReadAllText(manifestPath));

    Assert.Equal(BuiltInPetSkins.Castorice.Id, skin.Id, "Written manifest should preserve skin id.");
    Assert.Equal(BuiltInPetSkins.Castorice.DisplayName, skin.DisplayName, "Written manifest should preserve display name.");
    Assert.Equal(BuiltInPetSkins.Castorice.DefaultCharacterPath, skin.DefaultCharacterPath, "Written manifest should reload default character path.");
    Assert.Equal(BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle).FramePaths[0], skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "Written manifest should reload action frames.");
}

static void PetSkinManifestWriterStoresPathsRelativeToResourceRoot()
{
    using var temp = TempDirectory.Create();
    var manifestPath = System.IO.Path.Combine(temp.Path, "skin.json");

    PetSkinManifestWriter.WriteToFile(manifestPath, BuiltInPetSkins.Castorice);
    var json = File.ReadAllText(manifestPath);

    Assert.Contains(json, @"""resourceRoot"": ""Assets/Runtime/Castorice""", "Written manifest should keep the runtime resource root.");
    Assert.Contains(json, @"""defaultCharacter"": ""Castorice.png""", "Default character should be stored relative to resource root.");
    Assert.Contains(json, @"""States/Idle/Castorice.Idle.00.png""", "Action frame paths should be stored relative to resource root.");
}

static void PetSkinManifestRoundTripsOptionalPettingAction()
{
    var skin = PetSkinManifestLoader.LoadFromJson("""
        {
          "schemaVersion": 2,
          "id": "pettable",
          "displayName": "Pettable",
          "resourceRoot": "Skins/Pettable",
          "defaultCharacter": "Default.png",
          "actions": [
            { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
            { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
            { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] },
            { "id": "petting", "kind": "petting", "frameIntervalMs": 80, "frames": ["Petting/00.png", "Petting/01.png"] }
          ]
        }
        """);

    Assert.True(skin.TryGetAction(PetActionKind.Petting, out var petting), "Manifest should load optional petting actions.");
    Assert.Equal("Skins/Pettable/Petting/00.png", petting.FramePaths[0], "Petting paths should resolve under the resource root.");
    Assert.Equal(TimeSpan.FromMilliseconds(80), petting.FrameInterval, "Petting frame interval should load.");

    var json = PetSkinManifestWriter.ToJson(skin);
    Assert.Contains(json, @"""kind"": ""petting""", "Manifest writer should preserve optional petting actions.");
    var reloaded = PetSkinManifestLoader.LoadFromJson(json);
    Assert.True(reloaded.TryGetAction(PetActionKind.Petting, out _), "Written petting actions should remain loadable.");
}

static void PetSkinSelectionDefaultsToBuiltInSkin()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new PetSkinSelectionService(logger);

    var skin = service.LoadCurrentSkin(AppSettings.Default);

    Assert.Equal(BuiltInPetSkins.Castorice, skin, "No configured manifest should use the built-in skin.");
}

static void PetSkinSelectionLoadsConfiguredManifest()
{
    using var temp = TempDirectory.Create();
    var manifestDirectory = System.IO.Path.Combine(temp.Path, "CustomSkin");
    Directory.CreateDirectory(manifestDirectory);
    var manifestPath = System.IO.Path.Combine(manifestDirectory, "skin.json");
    File.WriteAllText(manifestPath, """
        {
          "schemaVersion": 1,
          "id": "configured",
          "displayName": "Configured Skin",
          "resourceRoot": "Resources",
          "defaultCharacter": "Default.png",
          "actions": [
            { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
            { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
            { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
          ]
        }
        """);
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new PetSkinSelectionService(logger);

    var skin = service.LoadCurrentSkin(new AppSettings { SkinManifestPath = manifestPath });

    Assert.Equal("configured", skin.Id, "Configured manifest should load as the active skin.");
    Assert.Equal(System.IO.Path.Combine(manifestDirectory, "Resources", "Default.png"), skin.DefaultCharacterPath, "Configured manifest paths should resolve from the manifest.");
}

static void PetSkinSelectionFallsBackWhenManifestFails()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new PetSkinSelectionService(logger);
    var missingManifest = System.IO.Path.Combine(temp.Path, "Missing", "skin.json");

    var skin = service.LoadCurrentSkin(new AppSettings { SkinManifestPath = missingManifest });

    Assert.Equal(BuiltInPetSkins.Castorice, skin, "Failed external manifest load should fall back to the built-in skin.");
    var logText = File.ReadAllText(paths.LogFile);
    Assert.Contains(logText, "Failed to load configured skin manifest", "Fallback should log the manifest load failure.");
}

static void AssetServiceDefaultsToBuiltInSkin()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new AssetService(logger);

    Assert.Equal(BuiltInPetSkins.Castorice, service.Skin, "Asset service should default to the built-in Castorice skin.");
}

static void AssetServiceUsesConfiguredSkinPaths()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var skin = BuiltInPetSkins.Castorice with
    {
        Id = "custom",
        DefaultCharacterPath = "Skins/Custom/Missing.png",
    };
    var service = new AssetService(logger, skin);

    _ = Assert.Throws<Exception>(() => service.LoadDefaultCharacter());

    var logText = File.ReadAllText(Directory.EnumerateFiles(paths.LogsDirectory, "*.log").Single());
    Assert.Contains(logText, "Skins/Custom/Missing.png", "Asset service should load the configured skin path.");
}

static void AssetServiceLoadsFileSystemSkinImagePaths()
{
    using var temp = TempDirectory.Create();
    var sourcePath = System.IO.Path.Combine(FindWorkspaceRoot(), "src", "CastoPet", "Assets", "Runtime", "Castorice", "Castorice.png");
    var skinImagePath = System.IO.Path.Combine(temp.Path, "Skin", "Default.png");
    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(skinImagePath)!);
    File.Copy(sourcePath, skinImagePath);
    var paths = new AppPaths(System.IO.Path.Combine(temp.Path, "Data"));
    var logger = new LoggingService(paths);
    var skin = BuiltInPetSkins.Castorice with
    {
        Id = "file-skin",
        DefaultCharacterPath = skinImagePath,
    };
    var service = new AssetService(logger, skin);

    var image = service.LoadDefaultCharacter();

    Assert.True(image.PixelWidth > 0, "File-system skin images should load through AssetService.");
}

static void AssetServiceLoadsExpressionImagesWithIsolatedTransitions()
{
    using var temp = TempDirectory.Create();
    var sourcePath = System.IO.Path.Combine(FindWorkspaceRoot(), "src", "CastoPet", "Assets", "Runtime", "Castorice", "Castorice.png");
    var finalPath = System.IO.Path.Combine(temp.Path, "Happy.png");
    var transitionPath = System.IO.Path.Combine(temp.Path, "Happy.00.png");
    File.Copy(sourcePath, finalPath);
    File.Copy(sourcePath, transitionPath);
    var paths = new AppPaths(System.IO.Path.Combine(temp.Path, "Data"));
    var logger = new LoggingService(paths);
    var expression = new PetExpressionDefinition(
        "happy",
        "Happy",
        finalPath,
        new[] { transitionPath, System.IO.Path.Combine(temp.Path, "Missing.png") },
        TimeSpan.FromMilliseconds(66));
    var skin = BuiltInPetSkins.Castorice with
    {
        Expressions = new[] { expression },
        Actions = BuiltInPetSkins.Castorice.Actions
            .Where(action => action.Kind is not (PetActionKind.ExpressionTransitionIn or PetActionKind.ExpressionTransitionOut))
            .ToArray(),
    };
    var service = new AssetService(logger, skin);

    var assets = service.LoadExpressionAssets();

    Assert.Equal(1, assets.Count, "A valid final expression image should remain available.");
    Assert.True(assets.ContainsKey(expression.Id), "Expression assets should be keyed directly by stable expression ID.");
    Assert.Equal(0, assets.Values.Single().TransitionFrames.Count, "One missing transition frame should discard only that transition sequence.");
    Assert.Equal(expression, assets.Values.Single().Definition, "Loaded expression assets should retain their definition.");
    Assert.Equal(0, service.LoadExpressionTransitionInFrames().Count, "A missing generic transition-in action should return no fallback frames.");
    Assert.Equal(0, service.LoadExpressionTransitionOutFrames().Count, "A missing generic transition-out action should return no fallback frames.");
}

static void AssetServiceTreatsMissingPettingFramesAsOptional()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var skin = BuiltInPetSkins.Castorice with
    {
        Actions = BuiltInPetSkins.Castorice.Actions
            .Where(action => action.Kind != PetActionKind.Petting)
            .ToArray(),
    };
    var service = new AssetService(logger, skin);

    Assert.Equal(0, service.LoadPettingFrames().Count, "Old skins without petting should use the runtime fallback instead of failing.");
}

static void BuiltInIdleActionDefinesEightAuthoredRateFramePaths()
{
    var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

    Assert.Equal(8, idle.FramePaths.Count, "Idle should use eight frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(125), idle.FrameInterval, "Idle frames should advance at the authored 8 FPS rate.");
    Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.00.png", idle.FramePaths[0], "First idle frame path should be zero padded.");
    Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.07.png", idle.FramePaths[^1], "Last idle frame path should be zero padded.");
}

static void IdleFrameDiagnosticsReadAllPackagedFrames()
{
    var diagnostics = ReadIdleFrameDiagnostics();

    Assert.Equal(BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle).FramePaths.Count, diagnostics.Count, "Diagnostics should include all idle frames.");
    Assert.True(diagnostics.All(frame => frame.Width == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display width.");
    Assert.True(diagnostics.All(frame => frame.Height == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display height.");
    Assert.True(diagnostics.All(frame => frame.Bounds.Width > 0 && frame.Bounds.Height > 0), "Idle frames should have visible alpha bounds.");
    Assert.True(diagnostics.Max(frame => frame.Bounds.Bottom) - diagnostics.Min(frame => frame.Bounds.Bottom) <= 1, "Idle frame bottom edges should stay anchored.");
    Assert.True(diagnostics.Max(frame => frame.CenterX) - diagnostics.Min(frame => frame.CenterX) <= 1.0, "Idle frame centers should stay horizontally anchored.");
    Assert.Equal("Castorice.Idle.07.png", diagnostics[^1].Name, "Diagnostics should preserve frame order.");
}

static void BuiltInBlinkActionDefinesRandomBlinkFrames()
{
    var blink = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Blink);

    Assert.Equal(3, blink.FramePaths.Count, "Blink should use three frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), blink.FrameInterval, "Blink frames should advance quickly.");
    Assert.Equal(TimeSpan.FromSeconds(3), blink.MinScheduleDelay, "Blink should not repeat too frequently.");
    Assert.Equal(TimeSpan.FromSeconds(7), blink.MaxScheduleDelay, "Blink should remain occasional.");
    Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.00.png", blink.FramePaths[0], "First blink frame path should be zero padded.");
    Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.02.png", blink.FramePaths[^1], "Last blink frame path should be zero padded.");
}

static void BuiltInMoveActionDefinesEightDistanceDrivenPaths()
{
    var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

    Assert.Equal(8, move.FramePaths.Count, "Move should use eight frames.");
    Assert.Equal(10d, move.DistancePerFrame, "Move frames should advance by travel distance.");
    Assert.Equal("Assets/Runtime/Castorice/States/Move/Castorice.Move.00.png", move.FramePaths[0], "First move frame path should be zero padded.");
    Assert.Equal("Assets/Runtime/Castorice/States/Move/Castorice.Move.07.png", move.FramePaths[^1], "Last move frame path should be zero padded.");
}

static void MoveFramePathsUseAppResources()
{
    var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

    for (var index = 0; index < move.FramePaths.Count; index++)
    {
        Assert.Equal($"Assets/Runtime/Castorice/States/Move/Castorice.Move.{index:00}.png", move.FramePaths[index], "Move frame should use the resource path convention.");
    }
}

static void MoveSpeedConstantsStayInSmoothRange()
{
    var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

    Assert.Equal(90d, move.BaseSpeedPixelsPerSecond, "Move speed should have a stable base.");
    Assert.Equal(80d, move.MinSpeedPixelsPerSecond, "Move speed lower bound should stay near the base.");
    Assert.Equal(105d, move.MaxSpeedPixelsPerSecond, "Move speed upper bound should stay near the base.");
}

static void ExpressionWheelDefinesEightItems()
{
    var expressions = BuiltInPetSkins.Castorice.Expressions;

    Assert.Equal(8, expressions.Count, "Built-in skin should use eight first-version expression wheel items.");
    Assert.Equal("Happy", expressions[0].Label, "First expression should be Happy.");
    Assert.Equal("Shy", expressions[1].Label, "Second expression should be Shy.");
    Assert.Equal("Sleepy", expressions[2].Label, "Third expression should be Sleepy.");
    Assert.Equal("Surprised", expressions[3].Label, "Fourth expression should be Surprised.");
    Assert.Equal("Pouting", expressions[4].Label, "Fifth expression should be Pouting.");
    Assert.Equal("Confused", expressions[5].Label, "Sixth expression should be Confused.");
    Assert.Equal("Proud", expressions[6].Label, "Seventh expression should be Proud.");
    Assert.Equal("Crying", expressions[7].Label, "Eighth expression should be Crying.");
    Assert.Equal(TimeSpan.FromMilliseconds(400), WheelCatalog.HoldDelay, "Wheel hold delay should leave room to distinguish a short click.");
    Assert.Equal(TimeSpan.FromSeconds(2), ExpressionWheelCatalog.ExpressionDuration, "Selected expression should be temporary.");
}

static void ExpressionWheelPathsUseAppResources()
{
    foreach (var item in BuiltInPetSkins.Castorice.Expressions)
    {
        var expected = $"Assets/Runtime/Castorice/Expressions/Castorice.Expression.{item.Label}.png";
        Assert.Equal(expected, item.ResourcePath, $"{item.Label} should use the expression resource path convention.");
    }
}

static void BuiltInExpressionTransitionActionsDefineSharedFrames()
{
    var transitionIn = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.ExpressionTransitionIn);
    var transitionOut = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.ExpressionTransitionOut);

    Assert.Equal(4, transitionIn.FramePaths.Count, "Transition-in should use four shared frames for smoother expression changes.");
    Assert.Equal(4, transitionOut.FramePaths.Count, "Transition-out should use four shared frames for smoother expression changes.");
    Assert.Equal(TimeSpan.FromMilliseconds(55), transitionIn.FrameInterval, "More transition frames should stay brief overall.");
    Assert.Equal(TimeSpan.FromMilliseconds(55), transitionOut.FrameInterval, "More transition frames should stay brief overall.");
}

static void ExpressionTransitionPathsUseAppResources()
{
    var transitionIn = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.ExpressionTransitionIn);
    var transitionOut = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.ExpressionTransitionOut);

    Assert.Equal("Assets/Runtime/Castorice/Expressions/Transition/Castorice.ExpressionTransition.In.00.png", transitionIn.FramePaths[0], "First transition-in path should use the transition resource convention.");
    Assert.Equal("Assets/Runtime/Castorice/Expressions/Transition/Castorice.ExpressionTransition.In.03.png", transitionIn.FramePaths[^1], "Last transition-in path should use the transition resource convention.");
    Assert.Equal("Assets/Runtime/Castorice/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png", transitionOut.FramePaths[0], "First transition-out path should use the transition resource convention.");
    Assert.Equal("Assets/Runtime/Castorice/Expressions/Transition/Castorice.ExpressionTransition.Out.03.png", transitionOut.FramePaths[^1], "Last transition-out path should use the transition resource convention.");
}

static void ExpressionTransitionPlannerPrefersSpecificReversibleFrames()
{
    var specific = new[] { "specific-0", "specific-1", "specific-2" };
    var fallbackIn = new[] { "fallback-in-0", "fallback-in-1" };
    var fallbackOut = new[] { "fallback-out-0", "fallback-out-1" };

    Assert.True(specific.SequenceEqual(ExpressionTransitionPlanner.EnterFrames(specific, fallbackIn)), "Specific enter frames should keep forward order.");
    Assert.True(new[] { "specific-2", "specific-1", "specific-0" }.SequenceEqual(ExpressionTransitionPlanner.ExitFrames(specific, fallbackOut)), "Specific exit frames should reverse the enter sequence.");
    Assert.True(fallbackIn.SequenceEqual(ExpressionTransitionPlanner.EnterFrames(Array.Empty<string>(), fallbackIn)), "Missing specific enter frames should use generic in frames.");
    Assert.True(fallbackOut.SequenceEqual(ExpressionTransitionPlanner.ExitFrames(Array.Empty<string>(), fallbackOut)), "Missing specific exit frames should keep generic out order.");
    Assert.Equal(0, ExpressionTransitionPlanner.EnterFrames(Array.Empty<string>(), Array.Empty<string>()).Count, "Missing specific and fallback frames should return an empty sequence.");
}

static void RadialWheelLayoutKeepsGenericTwoRingGeometry()
{
    Assert.Equal(8, WheelCatalog.MaxVisibleItemsPerRing, "Each radial ring should remain readable at no more than eight sectors.");
    Assert.True(WheelCatalog.InnerRadius < WheelCatalog.FirstRingOuterRadius, "The first ring should surround the center cancel zone.");
    Assert.True(WheelCatalog.FirstRingOuterRadius < WheelCatalog.SecondRingOuterRadius, "The second ring should surround the category ring.");
    Assert.Equal(28d, WheelCatalog.OuterExitTolerance, "The outer ring should allow a deliberate pointer overshoot.");
    Assert.Equal(238d, WheelCatalog.InteractionOuterRadius, "The interaction radius should include the visual radius and tolerance.");
    Assert.Equal(1.18d, WheelCatalog.SelectedScale, "Selected wheel text should still scale up visibly.");
}

static void RadialWheelStyleKeepsReadableRingHierarchy()
{
    var first = RadialWheelStyle.GetNormalFill(RadialWheelRing.First, isEnabled: true);
    var second = RadialWheelStyle.GetNormalFill(RadialWheelRing.Second, isEnabled: true);
    var firstDisabled = RadialWheelStyle.GetNormalFill(RadialWheelRing.First, isEnabled: false);
    var secondDisabled = RadialWheelStyle.GetNormalFill(RadialWheelRing.Second, isEnabled: false);

    Assert.Equal((byte)180, first.Alpha, "First-ring fill should be readable over the desktop.");
    Assert.Equal((byte)166, second.Alpha, "Second-ring fill should remain slightly lighter.");
    Assert.Equal((byte)110, firstDisabled.Alpha, "Disabled first-ring fill should remain subdued.");
    Assert.Equal((byte)96, secondDisabled.Alpha, "Disabled second-ring fill should remain subdued.");
    Assert.False(first.Equals(second), "The two normal ring fills should remain visually distinct.");
    Assert.True(RadialWheelStyle.SelectedFill.Alpha > first.Alpha, "Selection should be stronger than the first ring.");
    Assert.True(RadialWheelStyle.SelectedFill.Alpha > second.Alpha, "Selection should be stronger than the second ring.");
    Assert.Equal(0.016d, RadialWheelStyle.SectorGapRadians, "Sector dividers should use the refined gap.");
}

static void PointerGesturesClassifyLeftClickAndDrag()
{
    var classifier = new PetPointerGestureClassifier(6, 6, 14, TimeSpan.FromMilliseconds(400));
    var now = DateTimeOffset.Parse("2026-07-17T08:00:00Z");

    Assert.Equal(PetPointerIntent.None, classifier.Press(PetPointerButton.Left, 100, 100, now), "Left press should remain pending.");
    Assert.Equal(PetPointerIntent.None, classifier.Move(105.9, 105.9, now), "Movement below both axis thresholds should remain a click candidate.");
    Assert.Equal(PetPointerIntent.Petting, classifier.Release(PetPointerButton.Left, 105.9, 105.9, now), "Left release below threshold should pet.");

    classifier.Press(PetPointerButton.Left, 100, 100, now);
    Assert.Equal(PetPointerIntent.Drag, classifier.Move(106, 100, now), "Horizontal threshold should begin drag immediately.");
    Assert.Equal(PetPointerIntent.None, classifier.Release(PetPointerButton.Left, 106, 100, now), "Committed drag should not also pet on release.");

    classifier.Press(PetPointerButton.Left, 100, 100, now);
    Assert.Equal(PetPointerIntent.Drag, classifier.Move(100, 94, now), "Vertical threshold should begin drag immediately.");
}

static void PointerGesturesClassifyRightClickMovementAndHold()
{
    var classifier = new PetPointerGestureClassifier(6, 6, 14, TimeSpan.FromMilliseconds(400));
    var now = DateTimeOffset.Parse("2026-07-17T08:00:00Z");

    classifier.Press(PetPointerButton.Right, 50, 50, now);
    Assert.Equal(PetPointerIntent.None, classifier.Move(59, 59, now), "Radial movement below fourteen DIP should remain a menu candidate.");
    Assert.Equal(PetPointerIntent.ContextMenu, classifier.Release(PetPointerButton.Right, 59, 59, now.AddMilliseconds(399)), "Right release before hold delay should open the menu.");

    classifier.Press(PetPointerButton.Right, 50, 50, now);
    Assert.Equal(PetPointerIntent.RadialWheel, classifier.Move(64, 50, now), "Fourteen DIP movement should open the wheel without waiting.");

    classifier.Cancel();
    classifier.Press(PetPointerButton.Right, 50, 50, now);
    Assert.Equal(PetPointerIntent.None, classifier.UpdateHold(now.AddMilliseconds(399)), "Hold should remain pending before four hundred milliseconds.");
    Assert.Equal(PetPointerIntent.RadialWheel, classifier.UpdateHold(now.AddMilliseconds(400)), "Hold should open the wheel at four hundred milliseconds.");
}

static void PointerGesturesCancelConflictsAndCommitOnce()
{
    var classifier = new PetPointerGestureClassifier(6, 6, 14, TimeSpan.FromMilliseconds(400));
    var now = DateTimeOffset.Parse("2026-07-17T08:00:00Z");

    classifier.Press(PetPointerButton.Left, 0, 0, now);
    Assert.Equal(PetPointerIntent.None, classifier.Press(PetPointerButton.Right, 0, 0, now), "A second button should cancel the pending gesture.");
    Assert.Equal(PetPointerGestureState.Idle, classifier.State, "Conflicting buttons should return the classifier to idle.");

    classifier.Press(PetPointerButton.Right, 0, 0, now);
    Assert.Equal(PetPointerIntent.RadialWheel, classifier.Move(14, 0, now), "Movement should commit the wheel once.");
    Assert.Equal(PetPointerIntent.None, classifier.Move(20, 0, now), "Committed wheel movement should not emit a second intent.");
    classifier.Cancel();
    Assert.Equal(PetPointerGestureState.Idle, classifier.State, "Cancellation should clear committed state.");
}

static void WheelCatalogPreservesOrderedActionReferences()
{
    var expressions = new[]
    {
        new PetExpressionDefinition("happy", "Happy", "happy.png"),
        new PetExpressionDefinition("sleepy", "Sleepy", "sleepy.png"),
    };
    var shortcuts = new[]
    {
        new WheelActionItem("editor", "Editor", WheelActionType.Shortcut, "shortcut-editor"),
        new WheelActionItem("browser", "Browser", WheelActionType.Shortcut, "shortcut-browser"),
    };

    var catalog = WheelCatalogService.Create(expressions, shortcuts);

    Assert.Equal(2, catalog.Categories.Count, "The catalog should contain two categories.");
    Assert.Equal("expressions", catalog.Categories[0].Id, "Expressions should remain first.");
    Assert.Equal("shortcuts", catalog.Categories[1].Id, "Shortcuts should remain second.");
    Assert.Equal(WheelActionType.Expression, catalog.Categories[0].Items[0].ActionType, "Expression actions should be typed.");
    Assert.Equal("happy", catalog.Categories[0].Items[0].ActionReference, "Expression IDs should remain action references.");
    Assert.Equal("shortcut-editor", catalog.Categories[1].Items[0].ActionReference, "Shortcut references should be preserved.");
}

static void WheelCatalogExposesDisabledEmptyShortcutContent()
{
    var expressions = new[]
    {
        new PetExpressionDefinition("happy", "Happy", "happy.png"),
    };

    var shortcutCategory = WheelCatalogService.Create(expressions, []).Categories[1];

    Assert.Equal(1, shortcutCategory.Items.Count, "An empty shortcut category should contain guidance.");
    Assert.Equal(WheelActionType.Disabled, shortcutCategory.Items[0].ActionType, "Empty guidance should not be actionable.");
    Assert.False(shortcutCategory.Items[0].IsEnabled, "Empty shortcut guidance should be disabled.");
}

static void WheelCatalogServiceRefreshesSuccessfulShortcutMutations()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var shortcuts = new ShortcutService(paths, new LoggingService(paths));
    shortcuts.Load();
    var expressions = new[]
    {
        new PetExpressionDefinition("happy", "Happy", "happy.png"),
    };
    using var catalogs = new WheelCatalogService(expressions, shortcuts);
    var initial = catalogs.Current;
    var changed = 0;
    catalogs.Changed += (_, _) => changed++;

    Assert.True(shortcuts.TryAdd(new ShortcutDefinition("a", "Alpha", ShortcutType.File, @"C:\A.txt", "", null, 0)).Added, "First shortcut should be added.");
    var afterFirstAdd = catalogs.Current;
    Assert.False(ReferenceEquals(initial, afterFirstAdd), "Add should replace the catalog snapshot immediately.");
    Assert.Equal("a", afterFirstAdd.Categories[1].Items.Single().ActionReference, "Added shortcuts should appear without restarting.");

    Assert.True(shortcuts.TryAdd(new ShortcutDefinition("b", "Beta", ShortcutType.File, @"C:\B.txt", "", null, 0)).Added, "Second shortcut should be added.");
    var afterSecondAdd = catalogs.Current;
    Assert.False(ReferenceEquals(afterFirstAdd, afterSecondAdd), "Each successful add should publish a new snapshot.");
    Assert.Equal("a,b", string.Join(',', afterSecondAdd.Categories[1].Items.Select(item => item.ActionReference)), "Added shortcuts should preserve service order.");

    Assert.True(shortcuts.Rename("b", "Renamed").Succeeded, "Rename should succeed.");
    var afterRename = catalogs.Current;
    Assert.False(ReferenceEquals(afterSecondAdd, afterRename), "Rename should replace the catalog snapshot immediately.");
    Assert.Equal("Renamed", afterRename.Categories[1].Items[1].DisplayName, "Rename should update the wheel label immediately.");

    Assert.True(shortcuts.Move("b", 0).Succeeded, "Reorder should succeed.");
    var afterMove = catalogs.Current;
    Assert.False(ReferenceEquals(afterRename, afterMove), "Reorder should replace the catalog snapshot immediately.");
    Assert.Equal("b,a", string.Join(',', afterMove.Categories[1].Items.Select(item => item.ActionReference)), "Reorder should update wheel order immediately.");

    Assert.True(shortcuts.Delete("a").Succeeded, "Delete should succeed.");

    var shortcutItems = catalogs.Current.Categories.Single(category => category.Id == "shortcuts").Items;
    Assert.False(ReferenceEquals(afterMove, catalogs.Current), "Delete should replace the catalog snapshot immediately.");
    Assert.Equal(5, changed, "Every successful persisted mutation should publish one catalog change.");
    Assert.Equal(1, shortcutItems.Count, "Deleted shortcuts should disappear from the current snapshot.");
    Assert.Equal("b", shortcutItems[0].ActionReference, "Reordered shortcut identity should remain live.");
    Assert.Equal("Renamed", shortcutItems[0].DisplayName, "Renamed shortcuts should update wheel labels.");
}

static void WheelCatalogServiceUnsubscribesWhenDisposed()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var shortcuts = new ShortcutService(paths, new LoggingService(paths));
    shortcuts.Load();
    var catalogs = new WheelCatalogService(
        [new PetExpressionDefinition("happy", "Happy", "happy.png")],
        shortcuts);
    var snapshotAtDispose = catalogs.Current;
    var changed = 0;
    catalogs.Changed += (_, _) => changed++;

    catalogs.Dispose();
    Assert.True(shortcuts.TryAdd(new ShortcutDefinition("late", "Late", ShortcutType.File, @"C:\Late.txt", "", null, 0)).Added, "The shortcut service should remain independently usable.");

    Assert.True(ReferenceEquals(snapshotAtDispose, catalogs.Current), "A disposed catalog service must stop replacing snapshots.");
    Assert.Equal(0, changed, "A disposed catalog service must stop forwarding shortcut changes.");
}

static void ApplicationComposesOneSharedShortcutWheelGraph()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

    Assert.Equal(1, source.Split("new ShortcutService(_paths, _logger)", StringSplitOptions.None).Length - 1, "App startup should construct exactly one shortcut service from shared paths and logging.");
    Assert.Equal(1, source.Split("_shortcutService.Load();", StringSplitOptions.None).Length - 1, "App startup should load the shared shortcut service exactly once.");
    Assert.Contains(source, "new WheelCatalogService(skin.Expressions, _shortcutService)", "The live catalog should observe the shared shortcut service.");
    Assert.Equal(1, source.Split("new ShortcutDropHandler(_shortcutService)", StringSplitOptions.None).Length - 1, "App startup should construct one shared drop handler.");
    Assert.Equal(1, source.Split("new ShortcutLauncher(_logger)", StringSplitOptions.None).Length - 1, "App startup should construct one shared launcher.");
    Assert.Contains(source, "new PetWindow(assets, _logger, _wheelCatalogService, _shortcutService, _shortcutDropHandler, _shortcutLauncher)", "The production window should receive the shared service graph.");
    Assert.Contains(source, "_wheelCatalogService?.Dispose();", "Application shutdown should release the catalog subscription.");
}

static void PetWindowFollowsLiveWheelCatalogSnapshots()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));

    Assert.Contains(source, "WheelCatalogService wheelCatalogService", "PetWindow should receive the live catalog service instead of a frozen snapshot.");
    Assert.Contains(source, "_wheelCatalogService.Changed += OnWheelCatalogChanged;", "PetWindow should observe live catalog changes.");
    Assert.Contains(source, "Dispatcher.InvokeAsync(RefreshWheelCatalog)", "Catalog refresh should be marshalled to the window dispatcher.");
    Assert.Contains(source, "_wheelCatalog = _wheelCatalogService.Current;", "A refresh should install the latest catalog snapshot.");
    Assert.Contains(source, "_radialWheelController = new RadialWheelController(_wheelCatalog);", "A refresh should replace the controller bound to the old snapshot.");
    Assert.Contains(source, "CloseRadialWheel(cancelController: true", "Refreshing should safely close an open wheel before replacing state.");
    Assert.Contains(source, "BuildFirstRadialWheelRing();", "Refreshing should rebuild the category ring.");
    Assert.Contains(source, "_wheelCatalogService.Changed -= OnWheelCatalogChanged;", "Window close should unsubscribe from catalog changes.");
}

static void RadialWheelSelectorDistinguishesAllPointerRegions()
{
    Assert.Equal(RadialWheelRing.Center, RadialWheelSelector.GetSelection(0, 0, 2, 4).Ring, "Origin should be center.");
    var first = RadialWheelSelector.GetSelection(0, -80, 2, 4);
    Assert.Equal(RadialWheelRing.First, first.Ring, "First ring point should select a category.");
    Assert.Equal(0, first.SectorIndex, "Top should map to the first clockwise sector.");
    Assert.Equal(RadialWheelRing.Second, RadialWheelSelector.GetSelection(0, -170, 2, 4).Ring, "Outer ring point should select level two.");
    Assert.Equal(RadialWheelRing.Second, RadialWheelSelector.GetSelection(0, -220, 2, 4).Ring, "A slight outer overshoot should retain the outer-ring selection.");
    Assert.Equal(RadialWheelRing.Second, RadialWheelSelector.GetSelection(0, -238, 2, 4).Ring, "The tolerance boundary should remain interactive.");
    Assert.Equal(RadialWheelRing.Outside, RadialWheelSelector.GetSelection(0, -239, 2, 4).Ring, "A point beyond the outer tolerance should be outside.");
}

static void RadialWheelControllerHonorsCategoryDwell()
{
    var controller = new RadialWheelController(CreateWheelCatalog(3));
    var now = DateTimeOffset.UtcNow;
    controller.Open(now);
    controller.UpdatePointer(0, -80, now);
    controller.UpdatePointer(0, -80, now + TimeSpan.FromMilliseconds(119));
    Assert.False(controller.IsSecondLevelOpen, "Second level must remain closed before 120 ms.");
    controller.UpdatePointer(0, -80, now + TimeSpan.FromMilliseconds(120));
    Assert.True(controller.IsSecondLevelOpen, "Second level should open at 120 ms.");
    Assert.Equal(0, controller.SelectedCategoryIndex, "The dwelled category should remain selected.");
}

static void RadialWheelToleratesSlightOuterOvershoot()
{
    var controller = new RadialWheelController(CreateWheelCatalog(3));
    var now = DateTimeOffset.UtcNow;
    controller.Open(now);
    controller.UpdatePointer(0, -80, now);
    controller.UpdatePointer(0, -80, now + WheelCatalog.CategoryDwellDelay);

    controller.UpdatePointer(0, -220, now + WheelCatalog.CategoryDwellDelay + TimeSpan.FromMilliseconds(1));

    Assert.True(controller.IsOpen, "A slight overshoot should not close the wheel.");
    Assert.Equal(0, controller.SelectedSecondLevelIndex, "The overshoot area should preserve angular outer-ring selection.");

    controller.UpdatePointer(0, -239, now + WheelCatalog.CategoryDwellDelay + TimeSpan.FromMilliseconds(2));
    Assert.False(controller.IsOpen, "Moving beyond the tolerance should still close the wheel.");
}

static void RadialWheelControllerResetsAndCollapsesState()
{
    var controller = new RadialWheelController(CreateWheelCatalog(3));
    var now = DateTimeOffset.UtcNow;
    controller.Open(now);
    controller.UpdatePointer(0, -80, now);
    controller.UpdatePointer(-80, 0, now + TimeSpan.FromMilliseconds(119));
    controller.UpdatePointer(-80, 0, now + TimeSpan.FromMilliseconds(120));
    Assert.False(controller.IsSecondLevelOpen, "Changing category should reset dwell time.");
    controller.UpdatePointer(-80, 0, now + TimeSpan.FromMilliseconds(239));
    Assert.True(controller.IsSecondLevelOpen, "The replacement category should open after its own dwell.");

    controller.UpdatePointer(0, 0, now + TimeSpan.FromMilliseconds(240));
    Assert.False(controller.IsSecondLevelOpen, "Returning to center should collapse level two.");
    Assert.True(controller.IsOpen, "Center collapse should keep level one open.");
    controller.UpdatePointer(0, -239, now + TimeSpan.FromMilliseconds(241));
    Assert.False(controller.IsOpen, "Moving outside should cancel the wheel.");

    controller.Open(now);
    var result = controller.Cancel();
    Assert.Equal(WheelReleaseKind.Cancel, result.Kind, "Escape-style cancellation should return Cancel.");
    Assert.False(controller.IsOpen, "Cancellation should close the wheel.");
}

static void RadialWheelControllerPaginatesWithoutPersistingControls()
{
    foreach (var actionCount in new[] { 9, 15, 17 })
    {
        var catalog = CreateWheelCatalog(actionCount);
        var persistedIds = catalog.Categories[0].Items.Select(item => item.Id).ToArray();
        var controller = new RadialWheelController(catalog);
        var now = DateTimeOffset.UtcNow;
        controller.Open(now);
        controller.UpdatePointer(0, -80, now);
        controller.UpdatePointer(0, -80, now + WheelCatalog.CategoryDwellDelay);

        var visitedActions = new HashSet<string>();
        while (true)
        {
            Assert.True(controller.VisibleSecondLevelItems.Count <= WheelCatalog.MaxVisibleItemsPerRing, "Every rendered page must have at most eight sectors.");
            foreach (var item in controller.VisibleSecondLevelItems.Where(item => item.ActionType == WheelActionType.Shortcut))
            {
                visitedActions.Add(item.Id);
            }

            var nextIndex = controller.VisibleSecondLevelItems.ToList().FindIndex(item => item.ActionType == WheelActionType.NextPage);
            if (nextIndex < 0)
            {
                break;
            }

            var pageResult = controller.ReleaseSecondLevelItem(nextIndex);
            Assert.Equal(WheelReleaseKind.PageChanged, pageResult.Kind, "Page controls should update controller state.");
            Assert.True(controller.IsOpen, "Paging should keep the wheel open.");
        }

        Assert.Equal(actionCount, visitedActions.Count, "Pagination should expose every persisted action.");
        Assert.Equal(string.Join(',', persistedIds), string.Join(',', catalog.Categories[0].Items.Select(item => item.Id)), "Page controls must not be persisted in category actions.");
        Assert.False(catalog.Categories[0].Items.Any(item => item.ActionType is WheelActionType.PreviousPage or WheelActionType.NextPage), "Catalog data must not contain runtime page controls.");
    }
}

static WheelCatalog CreateWheelCatalog(int actionCount)
{
    var actions = Enumerable.Range(0, actionCount)
        .Select(index => new WheelActionItem($"action-{index}", $"Action {index}", WheelActionType.Shortcut, $"ref-{index}"))
        .ToArray();
    return new WheelCatalog([
        new WheelCategory("primary", "Primary", actions),
        new WheelCategory("secondary", "Secondary", [new WheelActionItem("other", "Other", WheelActionType.Shortcut, "other")]),
    ]);
}

static void ShortcutServiceLoadsEmptyStateAndRoundTrips()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));

    service.Load();
    Assert.Equal(0, service.GetAll().Count, "Missing storage should load as empty.");
    var added = service.TryAdd(new ShortcutDefinition("editor", "Editor", ShortcutType.Program, @"C:\Tools\Editor.exe", "--project demo", @"C:\Tools", 0));
    Assert.True(added.Added, "A valid shortcut should be added.");
    Assert.True(File.Exists(paths.ShortcutsFile), "Shortcut data should be persisted.");

    var reloaded = new ShortcutService(paths, new LoggingService(paths));
    reloaded.Load();
    var item = reloaded.GetAll().Single();
    Assert.Equal(@"C:\Tools\Editor.exe", item.Target, "Target should round trip separately.");
    Assert.Equal("--project demo", item.Arguments, "Arguments should remain separate from target.");
}

static void ShortcutServiceNormalizesDuplicateIdentities()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();

    Assert.True(service.TryAdd(new ShortcutDefinition("file-a", "File", ShortcutType.File, @"C:\Work\..\Work\Readme.txt", "", null, 0)).Added, "First path should be added.");
    Assert.False(service.TryAdd(new ShortcutDefinition("file-b", "File duplicate", ShortcutType.File, @"c:\work\README.TXT", "", null, 0)).Added, "Equivalent Windows paths should be duplicates.");
    Assert.True(service.TryAdd(new ShortcutDefinition("web-a", "Web", ShortcutType.WebUrl, "HTTPS://Example.com:443/docs/", "", null, 0)).Added, "First URL should be added.");
    Assert.False(service.TryAdd(new ShortcutDefinition("web-b", "Web duplicate", ShortcutType.WebUrl, "https://example.com/docs", "", null, 0)).Added, "Equivalent URLs should be duplicates.");
    Assert.True(service.TryAdd(new ShortcutDefinition("link-a", "Link", ShortcutType.WindowsShortcut, @"C:\Links\Tool.lnk", "", null, 0)).Added, "First link should be added.");
    Assert.False(service.TryAdd(new ShortcutDefinition("link-b", "Link duplicate", ShortcutType.WindowsShortcut, @"c:\links\TOOL.LNK", "", null, 0)).Added, "Link identity should use its own path.");
}

static void ShortcutServiceAppendsCandidatesWithContiguousOrdering()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();
    service.TryAdd(new ShortcutDefinition("first", "First", ShortcutType.File, @"C:\First.txt", "", null, 40));
    service.TryAdd(new ShortcutDefinition("second", "Second", ShortcutType.File, @"C:\Second.txt", "", null, -100));

    var entries = service.GetAll();
    Assert.Equal("first", entries[0].Id, "A candidate-provided sort order must not insert before existing entries.");
    Assert.Equal("second", entries[1].Id, "New shortcuts should append after existing entries.");
    Assert.True(new[] { 0, 1 }.SequenceEqual(entries.Select(entry => entry.SortOrder)), "Persisted sort orders should remain contiguous.");
}

static void ShortcutServiceMutatesOrderedEntries()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();
    service.TryAdd(new ShortcutDefinition("a", "A", ShortcutType.File, @"C:\A.txt", "", null, 5));
    service.TryAdd(new ShortcutDefinition("b", "B", ShortcutType.File, @"C:\B.txt", "", null, 2));

    Assert.Equal("a", service.GetAll()[0].Id, "New entries should append regardless of candidate sort order.");
    Assert.True(service.Rename("a", "Renamed").Succeeded, "Rename should succeed.");
    Assert.True(service.Move("b", 0).Succeeded, "Reorder should succeed.");
    Assert.Equal("b", service.GetAll()[0].Id, "Moved entry should occupy requested index.");
    Assert.Equal("Renamed", service.GetAll()[1].Name, "Rename should persist through reorder.");
    Assert.True(service.Delete("b").Succeeded, "Delete should succeed.");
    Assert.Equal(1, service.GetAll().Count, "Deleted entry should be removed.");
    Assert.Equal("Renamed", service.GetAll()[0].Name, "Remaining entry should preserve its edited name.");
}

static void ShortcutServiceUpdatesProgramLaunchOptionsSafely()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();
    var workingDirectory = System.IO.Path.Combine(temp.Path, "Workspace");
    Directory.CreateDirectory(workingDirectory);
    service.TryAdd(new ShortcutDefinition("program", "Program", ShortcutType.Program, @"C:\Tools\Editor.exe", "", null, 0));
    service.TryAdd(new ShortcutDefinition("file", "File", ShortcutType.File, @"C:\Notes.txt", "", null, 1));
    var changed = 0;
    service.Changed += (_, _) => changed++;

    var updated = service.UpdateLaunchOptions("program", "--project demo", $"  {workingDirectory}  ");

    Assert.True(updated.Succeeded, "Program launch options should be persisted.");
    var program = service.GetAll().Single(entry => entry.Id == "program");
    Assert.Equal("--project demo", program.Arguments, "Arguments should remain separate from the target.");
    Assert.Equal(workingDirectory, program.WorkingDirectory!, "Working directory should be trimmed before persistence.");
    Assert.Equal(1, changed, "A successful launch-option update should notify once.");

    var rejectedType = service.UpdateLaunchOptions("file", "--unsafe", workingDirectory);
    var rejectedDirectory = service.UpdateLaunchOptions("program", "changed", System.IO.Path.Combine(temp.Path, "Missing"));

    Assert.False(rejectedType.Succeeded, "Non-program entries must reject launch options.");
    Assert.False(rejectedDirectory.Succeeded, "A missing working directory must be rejected.");
    Assert.Equal(1, changed, "Rejected updates should not notify or mutate state.");
    program = service.GetAll().Single(entry => entry.Id == "program");
    Assert.Equal("--project demo", program.Arguments, "Rejected updates must preserve existing arguments.");
    Assert.Equal(2, service.GetAll().Count, "Validation must not auto-delete any shortcut.");
}

static void ShortcutServiceEnforcesEntryLimit()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();
    for (var index = 0; index < ShortcutService.MaxEntries; index++)
    {
        Assert.True(service.TryAdd(new ShortcutDefinition($"id-{index}", $"Item {index}", ShortcutType.File, $@"C:\Items\{index}.txt", "", null, index)).Added, "Entries within the limit should be added.");
    }

    Assert.False(service.TryAdd(new ShortcutDefinition("overflow", "Overflow", ShortcutType.File, @"C:\overflow.txt", "", null, 129)).Added, "The 129th entry should be rejected.");
    Assert.Equal(ShortcutService.MaxEntries, service.GetAll().Count, "Rejected entries must not alter state.");
}

static void ShortcutServiceRecoversMalformedStorage()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    Directory.CreateDirectory(paths.ShortcutsDirectory);
    File.WriteAllText(paths.ShortcutsFile, "{ malformed");
    var service = new ShortcutService(paths, new LoggingService(paths));

    service.Load();

    Assert.Equal(0, service.GetAll().Count, "Malformed storage should recover to empty.");
    Assert.Equal(1, Directory.GetFiles(paths.ShortcutsDirectory, "shortcuts.invalid-*.json").Length, "Malformed storage should receive a timestamped backup.");
}

static void ShortcutServiceIsolatesMalformedEntries()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    Directory.CreateDirectory(paths.ShortcutsDirectory);
    File.WriteAllText(paths.ShortcutsFile, """
        [
          { "id":"good", "name":"Good", "type":"File", "target":"C:\\\\Good.txt", "arguments":"", "workingDirectory":null, "sortOrder":0 },
          { "id":"bad", "name":42, "type":"Unknown", "target":null }
        ]
        """);
    var service = new ShortcutService(paths, new LoggingService(paths));

    service.Load();

    Assert.Equal(1, service.GetAll().Count, "A malformed entry should not discard valid siblings.");
    Assert.Equal("good", service.GetAll()[0].Id, "The valid entry should survive.");
}

static void ShortcutServiceNotifiesOnlyAfterPersistedMutations()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();
    var changed = 0;
    service.Changed += (_, _) => changed++;

    Assert.True(service.TryAdd(new ShortcutDefinition("a", "A", ShortcutType.File, @"C:\A.txt", "", null, 0)).Added, "Successful mutation should be reported.");
    Assert.Equal(1, changed, "Successful persisted mutation should notify once.");
    Assert.False(service.TryAdd(new ShortcutDefinition("duplicate", "A", ShortcutType.File, @"c:\a.TXT", "", null, 0)).Added, "Duplicate should fail without mutation.");
    Assert.Equal(1, changed, "Rejected mutation should not notify.");

    using var blocked = TempDirectory.Create();
    var blockedRoot = System.IO.Path.Combine(blocked.Path, "not-a-directory");
    File.WriteAllText(blockedRoot, "blocked");
    var blockedPaths = new AppPaths(blockedRoot);
    var blockedService = new ShortcutService(blockedPaths, new LoggingService(blockedPaths));
    var blockedChanges = 0;
    blockedService.Changed += (_, _) => blockedChanges++;
    var failed = blockedService.TryAdd(new ShortcutDefinition("failed", "Failed", ShortcutType.File, @"C:\Failed.txt", "", null, 0));
    Assert.False(failed.Succeeded, "File-system failures should be contained.");
    Assert.Equal(0, blockedChanges, "Failed persistence must not notify.");
    Assert.Equal(0, blockedService.GetAll().Count, "Failed persistence must not alter memory.");
}

static void ShortcutDropHandlerClassifiesExistingFileSystemItems()
{
    using var temp = TempDirectory.Create();
    var executablePath = System.IO.Path.Combine(temp.Path, "Editor.ExE");
    var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
    var folderPath = System.IO.Path.Combine(temp.Path, "Archive.exe");
    var linkPath = System.IO.Path.Combine(temp.Path, "Launch.lNk");
    File.WriteAllText(executablePath, "executable fixture");
    File.WriteAllText(filePath, "ordinary file fixture");
    Directory.CreateDirectory(folderPath);
    File.WriteAllText(linkPath, "shortcut fixture");
    var service = CreateShortcutService(temp.Path);
    var handler = new ShortcutDropHandler(service);

    var result = handler.AddDroppedItems([executablePath, filePath, folderPath, linkPath], []);

    Assert.Equal(4, result.AddedCount, "Every supported file-system item should be added.");
    Assert.Equal(0, result.DuplicateCount, "Distinct file-system items should not be duplicates.");
    Assert.Equal(0, result.UnsupportedCount, "Supported file-system items should not be rejected.");
    Assert.Equal(0, result.FailedCount, "Supported file-system items should not fail.");
    var entries = service.GetAll().ToDictionary(entry => entry.Target, StringComparer.OrdinalIgnoreCase);
    Assert.Equal(ShortcutType.Program, entries[executablePath].Type, "Executable extensions should be matched case-insensitively.");
    Assert.Equal("Editor", entries[executablePath].Name, "Executable names should omit their extension.");
    Assert.Equal(ShortcutType.File, entries[filePath].Type, "Ordinary files should remain files.");
    Assert.Equal("notes.txt", entries[filePath].Name, "Ordinary file names should retain their extension.");
    Assert.Equal(ShortcutType.Folder, entries[folderPath].Type, "Directories must be classified before their extension.");
    Assert.Equal(ShortcutType.WindowsShortcut, entries[linkPath].Type, "Windows shortcuts should retain their own type.");
    Assert.Equal(linkPath, entries[linkPath].Target, "Windows shortcuts should retain their own path.");
    Assert.Equal("executable fixture", File.ReadAllText(executablePath), "Drop recognition must not modify executable files.");
    Assert.Equal("ordinary file fixture", File.ReadAllText(filePath), "Drop recognition must not modify ordinary files.");
    Assert.True(Directory.Exists(folderPath), "Drop recognition must not move or remove directories.");
    Assert.Equal("shortcut fixture", File.ReadAllText(linkPath), "Drop recognition must not modify Windows shortcuts.");
}

static void ShortcutDropHandlerRejectsExecutableScripts()
{
    using var temp = TempDirectory.Create();
    var deniedExtensions = new[]
    {
        ".bat", ".CMD", ".Ps1", ".vbs", ".JS", ".jse",
        ".wsf", ".WSH", ".hta", ".COM", ".scr", ".MSI",
    };
    var deniedPaths = deniedExtensions
        .Select((extension, index) => System.IO.Path.Combine(temp.Path, $"Denied-{index}{extension}"))
        .ToArray();
    foreach (var path in deniedPaths)
    {
        File.WriteAllText(path, "denied fixture");
    }

    var textPath = System.IO.Path.Combine(temp.Path, "Readme.txt");
    var executablePath = System.IO.Path.Combine(temp.Path, "Editor.EXE");
    var linkPath = System.IO.Path.Combine(temp.Path, "Editor.LnK");
    File.WriteAllText(textPath, "text fixture");
    File.WriteAllText(executablePath, "executable fixture");
    File.WriteAllText(linkPath, "shortcut fixture");
    var service = CreateShortcutService(temp.Path);
    var handler = new ShortcutDropHandler(service);

    var result = handler.AddDroppedItems([.. deniedPaths, textPath, executablePath, linkPath], []);

    Assert.Equal(3, result.AddedCount, "Only the ordinary file, executable, and Windows shortcut should be added.");
    Assert.Equal(0, result.DuplicateCount, "Distinct safety fixtures should not be duplicates.");
    Assert.Equal(deniedPaths.Length, result.UnsupportedCount, "Every executable script or installer extension should be unsupported.");
    Assert.Equal(0, result.FailedCount, "Safety rejections should not be reported as storage failures.");
    var entries = service.GetAll();
    Assert.Equal(3, entries.Count, "Denied executable content must not be persisted.");
    Assert.Equal(ShortcutType.File, entries.Single(entry => entry.Target == textPath).Type, "Ordinary text files should remain allowed.");
    Assert.Equal(ShortcutType.Program, entries.Single(entry => entry.Target == executablePath).Type, "Explicit executable programs should remain allowed.");
    Assert.Equal(ShortcutType.WindowsShortcut, entries.Single(entry => entry.Target == linkPath).Type, "Windows shortcuts should remain explicitly allowed.");
    Assert.False(entries.Any(entry => deniedPaths.Contains(entry.Target, StringComparer.OrdinalIgnoreCase)), "Denied paths must never enter shortcut storage.");
    Assert.True(deniedPaths.All(File.Exists), "Rejected files must remain untouched.");
}

static void ShortcutDropHandlerAcceptsSafeWebTargets()
{
    using var temp = TempDirectory.Create();
    var internetShortcutPath = System.IO.Path.Combine(temp.Path, "Docs.URL");
    var internetShortcutContents = "[InternetShortcut]\r\n URL = https://example.com/docs \r\nIconIndex=0\r\n";
    File.WriteAllText(internetShortcutPath, internetShortcutContents);
    var service = CreateShortcutService(temp.Path);
    var handler = new ShortcutDropHandler(service);

    var result = handler.AddDroppedItems(
        [internetShortcutPath],
        [" HTTP://example.org/start ", "https://sub.example.net/path?q=1"]);

    Assert.Equal(3, result.AddedCount, "Internet shortcuts and direct HTTP/HTTPS text should be added.");
    Assert.Equal(0, result.DuplicateCount, "Distinct web targets should not be duplicates.");
    Assert.Equal(0, result.UnsupportedCount, "HTTP and HTTPS targets should be supported.");
    Assert.Equal(0, result.FailedCount, "Valid web targets should not fail.");
    var entries = service.GetAll();
    Assert.True(entries.All(entry => entry.Type == ShortcutType.WebUrl), "Every accepted web target should use the web URL type.");
    Assert.Equal("Docs", entries.Single(entry => entry.Target == "https://example.com/docs").Name, "Internet shortcuts should use their file name.");
    Assert.Equal("example.org", entries.Single(entry => entry.Target == "HTTP://example.org/start").Name, "Direct web targets should use a readable host name.");
    Assert.True(entries.All(entry => !string.IsNullOrWhiteSpace(entry.Id)), "Generated shortcut IDs should be non-empty.");
    Assert.Equal(internetShortcutContents, File.ReadAllText(internetShortcutPath), "Reading an internet shortcut must not modify it.");
}

static void ShortcutDropHandlerRejectsMissingAndUnsafeInputs()
{
    using var temp = TempDirectory.Create();
    var unsafeInternetShortcut = System.IO.Path.Combine(temp.Path, "Unsafe.url");
    var malformedInternetShortcut = System.IO.Path.Combine(temp.Path, "Malformed.url");
    File.WriteAllText(unsafeInternetShortcut, "[InternetShortcut]\nURL=ftp://example.com/file\n");
    File.WriteAllText(malformedInternetShortcut, "[InternetShortcut]\nIconIndex=0\n");
    var missingPath = System.IO.Path.Combine(temp.Path, "missing.txt");
    var service = CreateShortcutService(temp.Path);
    var handler = new ShortcutDropHandler(service);

    var result = handler.AddDroppedItems(
        [missingPath, unsafeInternetShortcut, malformedInternetShortcut, ""],
        ["ftp://example.com/file", "file:///C:/secret.txt", "javascript:alert(1)", "cmd.exe /c calc", "not a URL"]);

    Assert.Equal(0, result.AddedCount, "Unsafe and missing inputs must not be added.");
    Assert.Equal(0, result.DuplicateCount, "Rejected inputs are not duplicates.");
    Assert.Equal(9, result.UnsupportedCount, "Every missing, malformed, unsafe, or arbitrary input should be reported unsupported.");
    Assert.Equal(0, result.FailedCount, "Parser rejections should not be reported as storage failures.");
    Assert.Equal(0, service.GetAll().Count, "Rejected text must never become a command shortcut.");
    Assert.True(File.Exists(unsafeInternetShortcut), "Rejected internet shortcuts must remain untouched.");
    Assert.True(File.Exists(malformedInternetShortcut), "Malformed internet shortcuts must remain untouched.");
}

static void ShortcutDropHandlerAggregatesMixedBatchDuplicates()
{
    using var temp = TempDirectory.Create();
    var filePath = System.IO.Path.Combine(temp.Path, "Report.txt");
    var internetShortcutPath = System.IO.Path.Combine(temp.Path, "Portal.url");
    File.WriteAllText(filePath, "report");
    File.WriteAllText(internetShortcutPath, "[InternetShortcut]\nURL=https://example.com/portal/\n");
    var service = CreateShortcutService(temp.Path);
    var handler = new ShortcutDropHandler(service);

    var result = handler.AddDroppedItems(
        [filePath, filePath, internetShortcutPath],
        ["https://EXAMPLE.com:443/portal", "ftp://example.com/portal"]);

    Assert.Equal(2, result.AddedCount, "The mixed batch should add one file and one web target.");
    Assert.Equal(2, result.DuplicateCount, "Repeated paths and normalized URLs should be counted as duplicates.");
    Assert.Equal(1, result.UnsupportedCount, "The unsupported scheme should be counted once.");
    Assert.Equal(0, result.FailedCount, "The mixed batch should not contain storage failures.");
    Assert.Equal(5, result.AddedCount + result.DuplicateCount + result.UnsupportedCount + result.FailedCount, "Aggregate counts should concisely account for every dropped value.");
    Assert.Equal(2, service.GetAll().Count, "Duplicates and unsupported values must not create entries.");
}

static void ShortcutDropHandlerReportsShortcutLimitFailures()
{
    using var temp = TempDirectory.Create();
    var service = CreateShortcutService(temp.Path);
    for (var index = 0; index < ShortcutService.MaxEntries; index++)
    {
        var seed = new ShortcutDefinition($"seed-{index}", $"Seed {index}", ShortcutType.File, $@"C:\Seed\{index}.txt", "", null, index);
        Assert.True(service.TryAdd(seed).Added, "Limit test setup should fill the shortcut service.");
    }

    var overflowPath = System.IO.Path.Combine(temp.Path, "Overflow.exe");
    File.WriteAllText(overflowPath, "overflow fixture");
    var handler = new ShortcutDropHandler(service);

    var result = handler.AddDroppedItems([overflowPath], []);

    Assert.Equal(0, result.AddedCount, "Items beyond the shortcut limit must not be added.");
    Assert.Equal(0, result.DuplicateCount, "A new target beyond the limit is not a duplicate.");
    Assert.Equal(0, result.UnsupportedCount, "A valid executable remains supported at the limit.");
    Assert.Equal(1, result.FailedCount, "The shortcut limit should be reported as a failed addition.");
    Assert.Equal(ShortcutService.MaxEntries, service.GetAll().Count, "A limit failure must not alter stored entries.");
    Assert.Equal("overflow fixture", File.ReadAllText(overflowPath), "A limit failure must not modify the dropped executable.");
}

static ShortcutService CreateShortcutService(string baseDirectory)
{
    var paths = new AppPaths(baseDirectory);
    var service = new ShortcutService(paths, new LoggingService(paths));
    service.Load();
    return service;
}

static void ShortcutLauncherCreatesStructuredShellStartInfo()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var programPath = System.IO.Path.Combine(temp.Path, "Editor.EXE");
    var workingDirectory = System.IO.Path.Combine(temp.Path, "Workspace");
    File.WriteAllText(programPath, "program fixture");
    Directory.CreateDirectory(workingDirectory);
    var launcher = new ShortcutLauncher(new LoggingService(paths), _ => null);
    var program = new ShortcutDefinition(
        "editor",
        "Editor",
        ShortcutType.Program,
        programPath,
        "--project \"demo file\"",
        workingDirectory,
        0);

    var info = launcher.CreateStartInfo(program);

    Assert.Equal(program.Target, info.FileName, "Target must remain a structured filename.");
    Assert.Equal(program.Arguments, info.Arguments, "Arguments must remain separate from the target.");
    Assert.Equal(workingDirectory, info.WorkingDirectory, "An explicit working directory should be preserved.");
    Assert.True(info.UseShellExecute, "Windows shell behavior should open associated targets.");
    Assert.False(info.Verb.Equals("runas", StringComparison.OrdinalIgnoreCase), "Shortcut launching must never request elevation.");

    var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
    File.WriteAllText(filePath, "file fixture");
    var withoutWorkingDirectory = launcher.CreateStartInfo(
        new ShortcutDefinition("notes", "Notes", ShortcutType.File, filePath, "", null, 1));
    Assert.True(string.IsNullOrEmpty(withoutWorkingDirectory.WorkingDirectory), "Working directory should remain optional.");
}

static void ShortcutLauncherAcceptsEverySupportedTargetType()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var programPath = System.IO.Path.Combine(temp.Path, "Editor.exe");
    var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
    var folderPath = System.IO.Path.Combine(temp.Path, "Documents");
    var linkPath = System.IO.Path.Combine(temp.Path, "Editor.lnk");
    File.WriteAllText(programPath, "program fixture");
    File.WriteAllText(filePath, "file fixture");
    Directory.CreateDirectory(folderPath);
    File.WriteAllText(linkPath, "shortcut fixture");
    var startCount = 0;
    var launcher = new ShortcutLauncher(
        new LoggingService(paths),
        _ =>
        {
            startCount++;
            return null;
        });
    var definitions = new[]
    {
        new ShortcutDefinition("program", "Program", ShortcutType.Program, programPath, "", null, 0),
        new ShortcutDefinition("file", "File", ShortcutType.File, filePath, "", null, 1),
        new ShortcutDefinition("folder", "Folder", ShortcutType.Folder, folderPath, "", null, 2),
        new ShortcutDefinition("link", "Link", ShortcutType.WindowsShortcut, linkPath, "", null, 3),
        new ShortcutDefinition("http", "HTTP", ShortcutType.WebUrl, "http://example.com/start", "", null, 4),
        new ShortcutDefinition("https", "HTTPS", ShortcutType.WebUrl, "https://example.com/docs", "", null, 5),
    };

    foreach (var definition in definitions)
    {
        var result = launcher.Launch(definition);
        Assert.True(result.Succeeded, $"{definition.Type} should be launchable when its target is valid.");
    }

    Assert.Equal(definitions.Length, startCount, "Every valid definition should reach the injected process boundary once.");
}

static void ShortcutLauncherRejectsMissingAndMalformedDefinitions()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var existingFile = System.IO.Path.Combine(temp.Path, "not-a-program.txt");
    var existingExecutable = System.IO.Path.Combine(temp.Path, "program.exe");
    var existingFolder = System.IO.Path.Combine(temp.Path, "Folder");
    File.WriteAllText(existingFile, "file fixture");
    File.WriteAllText(existingExecutable, "program fixture");
    Directory.CreateDirectory(existingFolder);
    var startCount = 0;
    var launcher = new ShortcutLauncher(
        new LoggingService(paths),
        _ =>
        {
            startCount++;
            return null;
        });
    var invalidDefinitions = new[]
    {
        new ShortcutDefinition("missing-program", "Missing", ShortcutType.Program, System.IO.Path.Combine(temp.Path, "missing.exe"), "", null, 0),
        new ShortcutDefinition("wrong-program", "Wrong", ShortcutType.Program, existingFile, "", null, 1),
        new ShortcutDefinition("missing-workdir", "Workdir", ShortcutType.Program, existingExecutable, "", System.IO.Path.Combine(temp.Path, "MissingWorkdir"), 2),
        new ShortcutDefinition("missing-file", "Missing", ShortcutType.File, System.IO.Path.Combine(temp.Path, "missing.txt"), "", null, 2),
        new ShortcutDefinition("folder-as-file", "Wrong", ShortcutType.File, existingFolder, "", null, 3),
        new ShortcutDefinition("missing-folder", "Missing", ShortcutType.Folder, System.IO.Path.Combine(temp.Path, "MissingFolder"), "", null, 4),
        new ShortcutDefinition("file-as-folder", "Wrong", ShortcutType.Folder, existingFile, "", null, 5),
        new ShortcutDefinition("missing-link", "Missing", ShortcutType.WindowsShortcut, System.IO.Path.Combine(temp.Path, "missing.lnk"), "", null, 6),
        new ShortcutDefinition("wrong-link", "Wrong", ShortcutType.WindowsShortcut, existingFile, "", null, 7),
        new ShortcutDefinition("exe-as-file", "Wrong", ShortcutType.File, existingExecutable, "", null, 8),
        new ShortcutDefinition("ftp", "FTP", ShortcutType.WebUrl, "ftp://example.com/file", "", null, 9),
        new ShortcutDefinition("relative", "Relative", ShortcutType.WebUrl, "example.com/path", "", null, 10),
        new ShortcutDefinition("hostless", "Hostless", ShortcutType.WebUrl, "http:///path", "", null, 11),
        new ShortcutDefinition("unknown", "Unknown", (ShortcutType)999, existingFile, "", null, 12),
    };

    foreach (var definition in invalidDefinitions)
    {
        var result = launcher.Launch(definition);
        Assert.False(result.Succeeded, $"Invalid {definition.Id} definition should return a failure.");
        Assert.True(!string.IsNullOrWhiteSpace(result.Error), $"Invalid {definition.Id} definition should explain its failure.");
    }

    Assert.Equal(0, startCount, "Rejected definitions must never reach the process boundary.");
}

static void ShortcutLauncherRejectsTamperedExecutableFileDefinitions()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var deniedExtensions = new[]
    {
        ".bat", ".CMD", ".Ps1", ".vbs", ".JS", ".jse",
        ".wsf", ".WSH", ".hta", ".COM", ".scr", ".MSI",
    };
    var startCount = 0;
    var launcher = new ShortcutLauncher(
        new LoggingService(paths),
        _ =>
        {
            startCount++;
            return null;
        });

    foreach (var extension in deniedExtensions)
    {
        var path = System.IO.Path.Combine(temp.Path, $"Tampered{extension}");
        File.WriteAllText(path, "denied fixture");
        var definition = new ShortcutDefinition("tampered", "Tampered", ShortcutType.File, path, "", null, 0);

        var result = launcher.Launch(definition);

        Assert.False(result.Succeeded, $"A File definition must reject executable extension {extension}.");
        Assert.True(!string.IsNullOrWhiteSpace(result.Error), "A safety rejection should include an error message.");
    }

    var linkPath = System.IO.Path.Combine(temp.Path, "Allowed.lnk");
    File.WriteAllText(linkPath, "shortcut fixture");
    var linkResult = launcher.Launch(
        new ShortcutDefinition("link", "Link", ShortcutType.WindowsShortcut, linkPath, "", null, 0));
    Assert.True(linkResult.Succeeded, "An explicit Windows shortcut definition should remain allowed.");
    Assert.Equal(1, startCount, "Only the explicit Windows shortcut should reach the process boundary.");
}

static void ShortcutLauncherContainsAndLogsStartFailures()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
    File.WriteAllText(filePath, "file fixture");
    var launcher = new ShortcutLauncher(
        new LoggingService(paths),
        _ => throw new InvalidOperationException("simulated start failure"));

    var result = launcher.Launch(
        new ShortcutDefinition("notes", "Notes", ShortcutType.File, filePath, "", null, 0));

    Assert.False(result.Succeeded, "Process start exceptions should be contained as failures.");
    Assert.True(!string.IsNullOrWhiteSpace(result.Error), "Contained process failures should include an error message.");
    Assert.True(File.Exists(paths.LogFile), "Contained process failures should be logged.");
    var log = File.ReadAllText(paths.LogFile);
    Assert.Contains(log, "simulated start failure", "The log should retain the process exception details.");
    Assert.Contains(log, filePath, "The log should identify the target that failed to launch.");
}

static void PetWindowDefinesTwoLevelRadialOverlayAndDropSurface()
{
    var workspace = FindWorkspaceRoot();
    var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml"));

    Assert.Contains(xaml, "x:Name=\"RadialWheelOverlay\"", "The wheel should expose a generic radial overlay.");
    Assert.Contains(xaml, "x:Name=\"FirstRingSurface\"", "The overlay should expose a first-level category ring.");
    Assert.Contains(xaml, "x:Name=\"SecondRingSurface\"", "The overlay should expose a second-level action ring.");
    Assert.Contains(xaml, "AllowDrop=\"True\"", "The pet hit surface should accept shortcut drops.");
    Assert.Contains(xaml, "DragOver=\"OnPetDragOver\"", "The pet should validate incoming drop data.");
    Assert.Contains(xaml, "Drop=\"OnPetDrop\"", "The pet should register supported dropped shortcuts.");
}

static void PetWindowConsumesCentralizedRadialWheelStyling()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));
    var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml"));

    Assert.Contains(source, "RadialWheelStyle.GetNormalFill", "Initial and restored fills should use the shared style contract.");
    Assert.Contains(source, "visual.Ring", "Selection refresh should restore the visual's original ring style.");
    Assert.Contains(source, "RadialWheelStyle.SectorGapRadians", "Sector geometry should use the refined divider gap.");
    Assert.Contains(source, "RadialWheelStyle.LabelShadowOpacity", "Label shadows should use the refined style.");
    Assert.Contains(xaml, "Color=\"#C08A5EB4\"", "The center should use the vivid lavender highlight.");
    Assert.Contains(xaml, "Color=\"#B0F8E8FF\"", "The center should use the luminous glass edge highlight.");
}

static void PetWindowRoutesClassifiedPointerGestures()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));

    Assert.Contains(source, "PetPointerGestureClassifier", "PetWindow should delegate ambiguity to the tested gesture classifier.");
    Assert.Contains(source, "SystemParameters.MinimumHorizontalDragDistance", "Left drag should respect the Windows drag threshold.");
    Assert.Contains(source, "PetPointerIntent.Petting", "Left release should route to petting.");
    Assert.Contains(source, "PetPointerIntent.ContextMenu", "Right release should route to the manual context menu.");
    Assert.Contains(source, "ShowPetContextMenu", "The pet context menu should open only after classification.");
    Assert.Contains(source, "CaptureMouse()", "Pending gestures should retain pointer events outside the pet bounds.");
    Assert.False(source.Contains("        ContextMenu = menu;", StringComparison.Ordinal), "Automatic WPF context-menu opening should be disabled.");
}

static void PetWindowDefinesHoldFeedbackAndPettingPlayback()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));
    var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml"));

    Assert.Contains(xaml, "RadialWheelHoldOverlay", "Right hold should have a dedicated non-interactive progress overlay.");
    Assert.Contains(xaml, "RadialWheelHoldArc", "Hold feedback should expose an updateable circular arc.");
    Assert.Contains(source, "GetRightHoldProgress", "Hold feedback should use classifier timing.");
    Assert.Contains(source, "LoadPettingFrames", "PetWindow should load optional skin petting frames.");
    Assert.Contains(source, "AdvancePettingFrame", "Petting should play as a one-shot frame sequence.");
    Assert.Contains(source, "_isPetting", "Passive runtime modes should be able to gate on petting playback.");
}

static void PetWindowRoutesGenericRadialActions()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));

    Assert.Contains(source, "RadialWheelController", "PetWindow should delegate wheel state to the generic controller.");
    Assert.Contains(source, "DateTimeOffset.UtcNow", "Category dwell should be driven by explicit timestamps.");
    Assert.Contains(source, "WheelReleaseKind.PageChanged", "Page controls should keep the wheel open and refresh its page.");
    Assert.Contains(source, "WheelActionType.Expression", "Expression actions should be dispatched by generic action type.");
    Assert.Contains(source, "WheelActionType.Shortcut", "Shortcut actions should be dispatched by generic action type.");
    Assert.Contains(source, "_expressionAssetsById.TryGetValue", "Expression actions should resolve assets by expression ID.");
    Assert.Contains(source, "_shortcutLauncher.Launch", "Shortcut actions should use the injected launcher.");
    Assert.Contains(source, "WpfInput.Key.Escape", "Escape should cancel an open radial wheel.");
}

static void PetWindowExtractsNeutralShortcutDropData()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));

    Assert.Contains(source, "DataFormats.FileDrop", "WPF file-drop data should be extracted into paths.");
    Assert.Contains(source, "DataFormats.UnicodeText", "Unicode text drops should be extracted into neutral strings.");
    Assert.Contains(source, "UniformResourceLocator", "Browser URL clipboard formats should be recognized.");
    Assert.Contains(source, "_shortcutDrops.AddDroppedItems", "Only neutral path and text lists should cross into Core.");
    Assert.Contains(source, "DragDropEffects.Link", "Drop feedback should communicate that source files remain untouched.");
    Assert.False(source.Contains("File.Move(", StringComparison.Ordinal), "PetWindow must never move a dropped source file.");
}

static void PetWindowRetiresExpressionOnlyWheelIntegration()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));
    var selectorPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "Core", "ExpressionWheelSelector.cs");
    var catalogSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Core", "ExpressionWheelCatalog.cs"));

    Assert.False(source.Contains("ExpressionWheelSelector", StringComparison.Ordinal), "PetWindow should not use the retired expression-only selector.");
    Assert.False(source.Contains("ExpressionWheelItem", StringComparison.Ordinal), "PetWindow should resolve generic actions instead of expression-only wheel items.");
    Assert.False(source.Contains("ExpressionWheelSurface", StringComparison.Ordinal), "PetWindow should render generic ring surfaces.");
    Assert.False(File.Exists(selectorPath), "The obsolete expression-only selector should be deleted.");
    Assert.False(catalogSource.Contains("WheelDiameter", StringComparison.Ordinal), "ExpressionWheelCatalog should no longer own generic layout constants.");
    Assert.False(catalogSource.Contains("HoldDelay", StringComparison.Ordinal), "ExpressionWheelCatalog should retain only expression timing.");
    Assert.Contains(catalogSource, "ExpressionDuration", "The existing two-second expression duration should remain stable.");
}

static void SettingCatalogDefinesEveryBooleanSettingOnce()
{
    var definitions = SettingCatalog.Create(AppSettings.Default, SettingActions.None);

    Assert.Equal(
        "topmost,active-movement,click-through,push-cursor,input-reactive-mode,show-in-taskbar,start-with-windows",
        string.Join(',', definitions.Select(item => item.Id)),
        "The settings catalog should contain every boolean setting exactly once in group order.");
    Assert.Equal(
        "Behavior,Behavior,Interaction,Interaction,Interaction,System,System",
        string.Join(',', definitions.Select(item => item.Group)),
        "Settings should remain in stable behavior, interaction, and system groups.");
}

static void SettingCatalogExposesOnlyCommonDirectMenuSettings()
{
    var definitions = SettingCatalog.Create(AppSettings.Default, SettingActions.None);

    Assert.Equal(
        "topmost,click-through",
        string.Join(',', definitions.Where(item => item.ShowInDirectMenu).Select(item => item.Id)),
        "Only always-on-top and mouse click-through should remain in direct menus.");
}

static void SettingCatalogReadsSharedSettingsLive()
{
    var settings = AppSettings.Default;
    var definitions = SettingCatalog.Create(settings, SettingActions.None);
    var activeMovement = definitions.Single(item => item.Id == "active-movement");

    Assert.False(activeMovement.GetValue(), "Active movement should initially read false.");
    settings.ActiveMovement = true;
    Assert.True(activeMovement.GetValue(), "Definitions should read the shared settings instance instead of caching values.");
}

static void SettingsWindowServiceReusesTheOpenWindow()
{
    var created = 0;
    var window = new FakeSettingsWindow();
    using var service = new SettingsWindowService(() =>
    {
        created++;
        return window;
    });

    service.ShowOrActivate();
    service.ShowOrActivate();

    Assert.Equal(1, created, "Repeated settings commands should reuse the open window.");
    Assert.Equal(1, window.ShowCount, "The settings window should only be shown once.");
    Assert.Equal(2, window.ActivateCount, "Every settings command should activate the window.");
}

static void SettingsWindowServiceReleasesAClosedWindow()
{
    var windows = new List<FakeSettingsWindow>();
    using var service = new SettingsWindowService(() =>
    {
        var window = new FakeSettingsWindow();
        windows.Add(window);
        return window;
    });

    service.ShowOrActivate();
    windows[0].CloseFromUser();
    service.ShowOrActivate();

    Assert.Equal(2, windows.Count, "Opening settings after close should create a fresh window.");
}

static void SettingsWindowDefinesTheApprovedVisualStructure()
{
    var workspace = FindWorkspaceRoot();
    var xamlPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "SettingsWindow.xaml");
    var xaml = File.ReadAllText(xamlPath);

    Assert.Contains(xaml, "SettingsItemsHost", "The settings window should expose its catalog host.");
    Assert.Contains(xaml, "MiSans, Noto Sans SC, Microsoft YaHei UI", "The window should use the approved Chinese font stack.");
    Assert.Contains(xaml, "#8C7AA5", "Active controls should use dusty mist violet.");
    Assert.Contains(xaml, "#FAF9FC", "The main surface should use cool near-white.");
    Assert.False(xaml.Contains("#6F4AA8", StringComparison.Ordinal), "The old saturated purple should be removed.");
    Assert.Contains(xaml, "CloseButton", "The custom title bar should expose a close button.");
}

static void SettingsWindowExposesShortcutLauncherManagement()
{
    var workspace = FindWorkspaceRoot();
    var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "SettingsWindow.xaml"));

    Assert.Contains(xaml, "SettingsNavigation", "Settings should expose explicit segmented navigation.");
    Assert.Contains(xaml, "GeneralView", "Existing settings should remain available in a general view.");
    Assert.Contains(xaml, "ShortcutLauncherView", "Shortcut management should have its own view.");
    Assert.Contains(xaml, "ShortcutList", "Shortcut entries should render in a dedicated list.");
    Assert.Contains(xaml, "名称", "The shortcut list should label names concisely.");
    Assert.Contains(xaml, "类型", "The shortcut list should show item types.");
    Assert.Contains(xaml, "目标", "The shortcut list should show targets.");
    Assert.Contains(xaml, "状态", "The shortcut list should show validity.");
    Assert.Contains(xaml, "ToolTip=\"上移\"", "Move-up should be an icon-like button with a tooltip.");
    Assert.Contains(xaml, "ToolTip=\"下移\"", "Move-down should be an icon-like button with a tooltip.");
    Assert.Contains(xaml, "ToolTip=\"删除\"", "Delete should be an icon-like button with a tooltip.");
    Assert.Contains(xaml, "ShortcutNameTextBox", "The selected shortcut name should be editable.");
    Assert.Contains(xaml, "ShortcutArgumentsTextBox", "Program arguments should be editable.");
    Assert.Contains(xaml, "ShortcutWorkingDirectoryTextBox", "Program working directory should be editable.");
    Assert.Contains(xaml, "ShortcutUrlTextBox", "A manual URL input should be present.");
    Assert.Contains(xaml, "ShortcutUrlErrorText", "Unsafe URL validation should be shown inline.");
    Assert.Contains(xaml, "常规设置立即生效；快捷项编辑后请保存", "The footer should describe both immediate and explicit-save behavior accurately.");
}

static void SettingsWindowSharesShortcutServicesAndLiveUpdates()
{
    var workspace = FindWorkspaceRoot();
    var windowSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "SettingsWindow.xaml.cs"));
    var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

    Assert.Contains(windowSource, "ShortcutService shortcutService", "Settings should receive the shared shortcut service.");
    Assert.Contains(windowSource, "ShortcutDropHandler shortcutDropHandler", "Manual URL add should reuse safe shortcut parsing.");
    Assert.Contains(windowSource, "ShortcutLauncher shortcutLauncher", "Validity should reuse launcher validation.");
    Assert.Contains(windowSource, "_shortcutService.Changed +=", "Settings should refresh when shared shortcut data changes.");
    Assert.Contains(windowSource, "_shortcutService.Changed -=", "Settings should unsubscribe when the window closes.");
    Assert.Contains(windowSource, "_shortcutDropHandler.AddDroppedItems", "Manual URL add should use the existing HTTP/HTTPS-safe parser.");
    Assert.Contains(windowSource, "_shortcutService.Rename", "Name editing should persist through the shortcut service.");
    Assert.Contains(windowSource, "_shortcutService.Move", "Reorder actions should persist through the shortcut service.");
    Assert.Contains(windowSource, "_shortcutService.Delete", "Delete actions should persist through the shortcut service.");
    Assert.Contains(windowSource, "_shortcutService.UpdateLaunchOptions", "Program launch options should persist through the shortcut service.");
    Assert.Contains(appSource, "new SettingsWindow(commands, _crashReports, _updates, _shortcutService, _shortcutDropHandler, _shortcutLauncher)", "App should pass the same composed shortcut dependencies to settings.");
}

static void DirectMenusExposeTheSettingsCommand()
{
    Assert.Equal("设置", TrayService.SettingsText, "Direct menus should expose the settings window command.");
}

static void InputKeyboardLayoutMapsCommonKeys()
{
    Assert.True(InputKeyboardLayout.TryGetKeyBounds("A", out var a), "A should have a key rectangle.");
    Assert.True(InputKeyboardLayout.TryGetKeyBounds("Space", out var space), "Space should have a key rectangle.");
    Assert.True(InputKeyboardLayout.TryGetKeyBounds("Enter", out var enter), "Enter should have a key rectangle.");
    Assert.False(InputKeyboardLayout.TryGetKeyBounds("Unknown", out _), "Unknown keys should not map to a rectangle.");
    Assert.True(
        a.X >= 0 && a.Y >= 0 && a.Right <= InputKeyboardLayout.VisualWidth && a.Bottom <= InputKeyboardLayout.VisualHeight,
        "A should fit inside the visual bounds.");
    Assert.True(space.Width > a.Width, "Space should be wider than a letter key.");
    Assert.True(enter.Height >= a.Height, "Enter should be at least as tall as a letter key.");
}

static void InputKeyboardLayoutExposesDrawableKeys()
{
    Assert.True(InputKeyboardLayout.KeyIds.Contains("A"), "Drawable keys should include A.");
    Assert.True(InputKeyboardLayout.KeyIds.Contains("Space"), "Drawable keys should include Space.");
    Assert.True(InputKeyboardLayout.KeyIds.Contains("MouseLeft"), "Drawable keys should include mouse feedback zones.");
}

static void InputKeyboardLayoutExposesKeyLabels()
{
    Assert.Equal("A", InputKeyboardLayout.GetDisplayLabel("A"), "Letter keys should display themselves.");
    Assert.Equal("Space", InputKeyboardLayout.GetDisplayLabel("Space"), "Space should display a readable label.");
    Assert.Equal("←", InputKeyboardLayout.GetDisplayLabel("Left"), "Arrow keys should display arrow glyphs.");
    Assert.Equal("L", InputKeyboardLayout.GetDisplayLabel("MouseLeft"), "Mouse left feedback should display a compact label.");
}

static void InputReactiveStateExpiresHighlights()
{
    var state = new InputReactiveState();

    state.AddKey("A", TimeSpan.Zero);

    Assert.True(state.GetActiveHighlights(TimeSpan.FromMilliseconds(100)).Contains("A"), "A should remain active before expiration.");
    Assert.False(state.GetActiveHighlights(TimeSpan.FromMilliseconds(300)).Contains("A"), "A should expire after the highlight duration.");
}

static void WindowsInputHookNormalizesCommonKeys()
{
    Assert.Equal("A", WindowsInputHookService.NormalizeVirtualKey(0x41), "VK_A should normalize to A.");
    Assert.Equal("Space", WindowsInputHookService.NormalizeVirtualKey(0x20), "VK_SPACE should normalize to Space.");
    Assert.Equal("Enter", WindowsInputHookService.NormalizeVirtualKey(0x0D), "VK_RETURN should normalize to Enter.");
    Assert.Equal("Left", WindowsInputHookService.NormalizeVirtualKey(0x25), "VK_LEFT should normalize to Left.");
}

static void InputReactiveModeSuppressesPassiveAnimation()
{
    Assert.False(
        InputReactiveModePolicy.AllowsPassiveAnimation(inputReactiveModeActive: true),
        "Input reactive mode should pause idle, blink, and active movement visuals.");
    Assert.True(
        InputReactiveModePolicy.AllowsPassiveAnimation(inputReactiveModeActive: false),
        "Normal passive animation should remain available outside input reactive mode.");
}

static void MovementPlannerClampsTargetsToWorkArea()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);

    var target = PetMovementPlanner.ClampTarget(
        left: 460,
        top: 390,
        windowWidth: 100,
        windowHeight: 120,
        bounds);

    Assert.Equal(400d, target.Left, "Target left should keep the full pet inside the work area.");
    Assert.Equal(280d, target.Top, "Target top should keep the full pet inside the work area.");
}

static void MovementPlannerApproachesMouseWithCursorOffset()
{
    var bounds = new PetMovementBounds(0, 0, 800, 600);

    var target = PetMovementPlanner.CalculateMouseApproachTarget(
        petLeft: 100,
        petTop: 100,
        petWidth: 100,
        petHeight: 100,
        mouseX: 300,
        mouseY: 150,
        bounds);

    var targetCenterX = target.Left + 50;
    var targetCenterY = target.Top + 50;
    var distance = Math.Sqrt(Math.Pow(targetCenterX - 300, 2) + Math.Pow(targetCenterY - 150, 2));

    Assert.True(distance >= PetMovementPlanner.MinMouseApproachOffset, "Target should not cover the cursor.");
    Assert.True(distance <= PetMovementPlanner.MaxMouseApproachOffset, "Target should stop close to the cursor.");
    Assert.True(target.Left > 100, "Target should move toward the mouse.");
}

static void MovementPlannerEasesTowardTarget()
{
    var next = PetMovementPlanner.StepToward(
        currentLeft: 0,
        currentTop: 0,
        target: new PetMovementTarget(100, 50));

    Assert.True(next.Left > 0, "Next left should move forward.");
    Assert.True(next.Left < 100, "Next left should ease instead of jumping.");
    Assert.True(next.Top > 0, "Next top should move forward.");
    Assert.True(next.Top < 50, "Next top should ease instead of jumping.");
}

static void MovementPlannerDetectsCloseTargets()
{
    var target = new PetMovementTarget(12, 16);

    Assert.True(PetMovementPlanner.IsClose(10, 14, target), "Nearby coordinates should be close.");
    Assert.False(PetMovementPlanner.IsClose(0, 0, target), "Distant coordinates should not be close.");
}

static void MovementPlannerDetectsMouseApproachRestPosition()
{
    var bounds = new PetMovementBounds(0, 0, 800, 600);
    var target = PetMovementPlanner.CalculateMouseApproachTarget(
        petLeft: 100,
        petTop: 100,
        petWidth: 100,
        petHeight: 100,
        mouseX: 300,
        mouseY: 150,
        bounds);

    Assert.True(
        PetMovementPlanner.IsAtMouseApproachTarget(
            target.Left + 1,
            target.Top + 1,
            petWidth: 100,
            petHeight: 100,
            mouseX: 300,
            mouseY: 150,
            bounds),
        "Pet should be treated as stationary when already near the mouse approach target.");
    Assert.False(
        PetMovementPlanner.IsAtMouseApproachTarget(
            petLeft: 100,
            petTop: 100,
            petWidth: 100,
            petHeight: 100,
            mouseX: 300,
            mouseY: 150,
            bounds),
        "Pet should still move when away from the mouse approach target.");
}

static void CursorNudgePlannerNudgesNearbyCursor()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);
    var result = CursorNudgePlanner.CalculateNudge(
        cursorX: 120,
        cursorY: 120,
        petCenterX: 130,
        petCenterY: 120,
        movementDeltaX: 10,
        movementDeltaY: 0,
        bounds);

    Assert.True(result.ShouldMove, "Nearby cursor should be nudged.");
    Assert.Equal(123d, result.X, "Nudge should clamp to the per-frame maximum.");
    Assert.Equal(120d, result.Y, "Horizontal movement should not change Y.");
}

static void CursorNudgePlannerIgnoresDistantCursor()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);
    var result = CursorNudgePlanner.CalculateNudge(
        cursorX: 20,
        cursorY: 20,
        petCenterX: 200,
        petCenterY: 200,
        movementDeltaX: 10,
        movementDeltaY: 0,
        bounds);

    Assert.False(result.ShouldMove, "Distant cursor should not be nudged.");
}

static void CursorNudgePlannerClampsToWorkArea()
{
    var bounds = new PetMovementBounds(0, 0, 100, 100);
    var result = CursorNudgePlanner.CalculateNudge(
        cursorX: 99,
        cursorY: 99,
        petCenterX: 98,
        petCenterY: 98,
        movementDeltaX: 10,
        movementDeltaY: 10,
        bounds);

    Assert.True(result.ShouldMove, "Nearby cursor should still be nudged at the edge.");
    Assert.Equal(99d, result.X, "Cursor should stay inside the work area.");
    Assert.Equal(99d, result.Y, "Cursor should stay inside the work area.");
}

static void CursorNudgePlannerDetectsManualMovementCooldown()
{
    Assert.True(
        CursorNudgePlanner.IsManualMovement(
            currentX: 140,
            currentY: 100,
            expectedX: 100,
            expectedY: 100),
        "Large unexpected cursor movement should count as manual input.");
    Assert.False(
        CursorNudgePlanner.CanNudgeAfterManualMovement(
            now: TimeSpan.FromMilliseconds(500),
            lastManualMovement: TimeSpan.Zero),
        "Push should pause during the manual movement cooldown.");
    Assert.True(
        CursorNudgePlanner.CanNudgeAfterManualMovement(
            now: TimeSpan.FromMilliseconds(1200),
            lastManualMovement: TimeSpan.Zero),
        "Push should resume after the manual movement cooldown.");
}

static void CursorNudgePlannerBlocksWhileMouseButtonIsPressed()
{
    Assert.False(
        CursorNudgePlanner.CanNudge(
            isMouseButtonPressed: true,
            now: TimeSpan.FromSeconds(2),
            lastManualMovement: null,
            pushStartedAt: TimeSpan.FromSeconds(1)),
        "Cursor push should stop while any mouse button is pressed.");
}

static void CursorNudgePlannerLimitsContinuousPushDuration()
{
    Assert.True(
        CursorNudgePlanner.CanNudge(
            isMouseButtonPressed: false,
            now: TimeSpan.FromMilliseconds(500),
            lastManualMovement: null,
            pushStartedAt: TimeSpan.Zero),
        "Cursor push should be allowed before the continuous duration cap.");
    Assert.False(
        CursorNudgePlanner.CanNudge(
            isMouseButtonPressed: false,
            now: TimeSpan.FromMilliseconds(2500),
            lastManualMovement: null,
            pushStartedAt: TimeSpan.Zero),
        "Cursor push should stop after the continuous duration cap.");
}

static void PetAnimationTimingsAreResponsive()
{
    Assert.Equal(TimeSpan.FromMilliseconds(120), PetAnimationTimings.ExpressionEnterDuration, "Expression enter should be quick.");
    Assert.Equal(TimeSpan.FromMilliseconds(180), PetAnimationTimings.ExpressionExitDuration, "Expression exit should be smooth but short.");
    Assert.Equal(TimeSpan.FromMilliseconds(120), PetAnimationTimings.WheelOpenDuration, "Wheel open should feel immediate.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), PetAnimationTimings.WheelSelectionDuration, "Selection emphasis should respond quickly.");
    Assert.Equal(TimeSpan.FromMilliseconds(250), PetAnimationTimings.ActiveMovementProbeInterval, "Active movement should use a low-frequency stationary probe.");
}

static void IdleBreathingValuesAreNeutralDuringStabilization()
{
    Assert.Equal(TimeSpan.FromMilliseconds(1900), PetAnimationTimings.IdleBreathingCycleDuration, "Idle breathing cycle duration should stay available for later tuning.");
    Assert.Equal(0d, PetAnimationTimings.IdleBreathingTranslateY, "Idle breathing vertical movement should be disabled while stabilizing frame anchors.");
    Assert.Equal(0d, PetAnimationTimings.IdleBreathingScaleDelta, "Idle breathing scale should be disabled while stabilizing frame anchors.");
    Assert.Equal(0.96, PetAnimationTimings.ExpressionDimmedOpacity, "Expression transition should only slightly dim during swaps.");
    Assert.Equal(0.92, PetAnimationTimings.WheelOpenStartScale, "Wheel should open from a small scale change.");
}

static void CharacterStationaryAnimationsAreEnabled()
{
    Assert.True(PetAnimationTimings.CharacterFrameAnimationEnabled, "Idle frame animation should be restored only while stationary.");
    Assert.True(PetAnimationTimings.BlinkFrameAnimationEnabled, "Blink should be restored while stationary.");
    Assert.True(PetAnimationTimings.ActiveMovementScaleDelta > 0, "Active movement should use a subtle visual state after static movement validated window smoothness.");
    Assert.True(PetAnimationTimings.ActiveMovementScaleDelta <= 0.006, "Active movement scale should stay subtle.");
    Assert.True(PetAnimationTimings.DragMovementScaleDelta > PetAnimationTimings.ActiveMovementScaleDelta, "Dragging should use a slightly stronger visual state than automatic movement.");
    Assert.True(PetAnimationTimings.DragMovementScaleDelta <= 0.012, "Dragging scale should stay subtle.");
}

static void CharacterAssetsDecodeAtPetDisplayWidth()
{
    Assert.Equal(320, AssetService.CharacterDecodePixelWidth, "Character assets should decode near their display width to avoid full-size frame memory.");
}

static void AssetDiagnosticsIncludeGroupAndResourcePath()
{
    var message = AssetService.FormatLoadFailureMessage("Idle frames", "Assets/Runtime/Castorice/States/Idle/Castorice.Idle.03.png");

    Assert.Contains(message, "Idle frames", "Asset diagnostics should include the resource group.");
    Assert.Contains(message, "Assets/Runtime/Castorice/States/Idle/Castorice.Idle.03.png", "Asset diagnostics should include the resource path.");
}

static void InputReactiveAssetPathUsesAppResource()
{
    Assert.Equal(
        "Assets/Runtime/Castorice/States/InputReactive/Castorice.InputReactive.Base.png",
        AssetService.InputReactiveBasePath,
        "Input reactive base should use an app resource path.");
}

static void InputReactiveAssetIsPackaged()
{
    var workspace = FindWorkspaceRoot();
    var projectFile = System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj");
    var projectText = File.ReadAllText(projectFile);

    Assert.Contains(
        projectText,
        @"Assets\Runtime\Castorice\States\InputReactive\Castorice.InputReactive.Base.png",
        "Input reactive base should be packaged as a WPF resource.");
}

static void PackagedCharacterAssetsAreDisplaySized()
{
    var workspace = FindWorkspaceRoot();
    var assetsRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "Runtime", "Castorice");
    var excludedSegments = Array.Empty<string>();
    var assets = Directory
        .EnumerateFiles(assetsRoot, "*.png", SearchOption.AllDirectories)
        .Where(path => !System.IO.Path.GetFileName(path).Equals("blink-preview.png", StringComparison.OrdinalIgnoreCase))
        .Where(path => !excludedSegments.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)));

    foreach (var asset in assets)
    {
        var (width, height) = ReadPngSize(asset);

        Assert.True(
            width <= AssetService.CharacterDecodePixelWidth && height <= 420,
            $"{asset} should fit the pet display bounds, got {width}x{height}.");
    }
}

static void PackagedExpressionTransitionsHaveCompleteSourceAndRuntimeEndpoints()
{
    var workspace = FindWorkspaceRoot();
    var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
    var labels = new[] { "Happy", "Shy", "Sleepy", "Surprised", "Pouting", "Confused", "Proud", "Crying" };
    var idlePath = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "Idle", "Castorice.Idle.00.png");
    var idleBytes = File.ReadAllBytes(idlePath);

    foreach (var label in labels)
    {
        var id = label.ToLowerInvariant();
        var targetPath = System.IO.Path.Combine(projectRoot, "Assets", "Skins", "Castorice", "expressions", "targets", $"{label}.png");
        var projectPath = System.IO.Path.Combine(projectRoot, "Assets", "Skins", "Castorice", "actions", "expressions", $"{id}.transition.animator.json");
        var finalPath = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "Expressions", $"Castorice.Expression.{label}.png");
        var transitionRoot = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "Expressions", label, "Transition");
        Assert.True(File.Exists(targetPath), $"{label} should keep a CastoPet-owned source target.");
        Assert.True(File.Exists(projectPath), $"{label} should keep an editable transition project.");

        var frames = Enumerable.Range(0, 6)
            .Select(index => System.IO.Path.Combine(transitionRoot, $"Castorice.Expression.{label}.Transition.{index:00}.png"))
            .ToArray();
        Assert.True(frames.All(File.Exists), $"{label} should include six consecutive runtime transition frames.");
        foreach (var frame in frames)
        {
            using var bitmap = new Bitmap(frame);
            Assert.Equal(320, bitmap.Width, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels wide.");
            Assert.Equal(320, bitmap.Height, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels high.");
        }
        Assert.True(idleBytes.SequenceEqual(File.ReadAllBytes(frames[0])), $"{label} transition frame 00 should equal Idle.00.");
        Assert.True(File.ReadAllBytes(finalPath).SequenceEqual(File.ReadAllBytes(frames[^1])), $"{label} transition frame 05 should equal its final expression image.");
    }

    var projectText = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
    Assert.Contains(projectText, @"Assets\Runtime\Castorice\Expressions\*\Transition\*.png", "Expression transition PNGs should be packaged with a WPF resource glob.");
}

static IReadOnlyList<IdleFrameDiagnostic> ReadIdleFrameDiagnostics()
{
    var workspace = FindWorkspaceRoot();
    var idleRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "Runtime", "Castorice", "States", "Idle");
    var frames = Directory
        .EnumerateFiles(idleRoot, "Castorice.Idle.*.png", SearchOption.TopDirectoryOnly)
        .OrderBy(System.IO.Path.GetFileName, StringComparer.Ordinal)
        .ToArray();

    var diagnostics = new List<IdleFrameDiagnostic>();
    for (var index = 0; index < frames.Length; index++)
    {
        using var bitmap = new Bitmap(frames[index]);
        var bounds = FindVisibleBounds(bitmap);
        diagnostics.Add(new IdleFrameDiagnostic(
            Name: System.IO.Path.GetFileName(frames[index]),
            Width: bitmap.Width,
            Height: bitmap.Height,
            Bounds: bounds,
            CenterX: bounds.Left + bounds.Width / 2d,
            AdjacentAverageDelta: 0));
    }

    for (var index = 0; index < diagnostics.Count; index++)
    {
        var current = frames[index];
        var next = frames[(index + 1) % frames.Length];
        using var currentBitmap = new Bitmap(current);
        using var nextBitmap = new Bitmap(next);
        diagnostics[index] = diagnostics[index] with
        {
            AdjacentAverageDelta = CalculateAverageRgbaDelta(currentBitmap, nextBitmap),
        };
    }

    return diagnostics;
}

static Rectangle FindVisibleBounds(Bitmap bitmap)
{
    var minX = bitmap.Width;
    var minY = bitmap.Height;
    var maxX = -1;
    var maxY = -1;

    for (var y = 0; y < bitmap.Height; y++)
    {
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x, y).A <= 8)
            {
                continue;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
    }

    if (maxX < minX || maxY < minY)
    {
        return Rectangle.Empty;
    }

    return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
}

static double CalculateAverageRgbaDelta(Bitmap current, Bitmap next)
{
    if (current.Width != next.Width || current.Height != next.Height)
    {
        throw new InvalidOperationException("Idle frames must have matching dimensions.");
    }

    long total = 0;
    long samples = 0;
    for (var y = 0; y < current.Height; y += 2)
    {
        for (var x = 0; x < current.Width; x += 2)
        {
            var a = current.GetPixel(x, y);
            var b = next.GetPixel(x, y);
            total += Math.Abs(a.R - b.R);
            total += Math.Abs(a.G - b.G);
            total += Math.Abs(a.B - b.B);
            total += Math.Abs(a.A - b.A);
            samples++;
        }
    }

    return total / (samples * 4d);
}

static string FindWorkspaceRoot()
{
    var current = new DirectoryInfo(Environment.CurrentDirectory);
    while (current is not null)
    {
        if (File.Exists(System.IO.Path.Combine(current.FullName, "CastoPet.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find workspace root.");
}

static (int Width, int Height) ReadPngSize(string path)
{
    Span<byte> header = stackalloc byte[24];
    using var stream = File.OpenRead(path);
    if (stream.Read(header) != header.Length)
    {
        throw new InvalidOperationException($"{path} is not a valid PNG.");
    }

    var width = ReadBigEndianInt32(header[16..20]);
    var height = ReadBigEndianInt32(header[20..24]);
    return (width, height);
}

static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
{
    return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
}

static class Assert
{
    public static void True(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void False(bool value, string message)
    {
        if (value) throw new InvalidOperationException(message);
    }

    public static void Contains(string text, string expected, string message)
    {
        if (!text.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }

    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }

        throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
    }
}

sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    private TempDirectory(string path)
    {
        Path = path;
    }

    public static TempDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CastoPet.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

readonly record struct IdleFrameDiagnostic(
    string Name,
    int Width,
    int Height,
    Rectangle Bounds,
    double CenterX,
    double AdjacentAverageDelta);

sealed class FakeSettingsWindow : ISettingsWindow
{
    public event EventHandler? Closed;

    public bool IsVisible { get; private set; }
    public int ShowCount { get; private set; }
    public int ActivateCount { get; private set; }

    public void Show()
    {
        ShowCount++;
        IsVisible = true;
    }

    public bool Activate()
    {
        ActivateCount++;
        return true;
    }

    public void Close()
    {
        CloseFromUser();
    }

    public void CloseFromUser()
    {
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

sealed class FakeUpdateService : IUpdateService
{
    public bool IsInstalled { get; set; } = true;
    public string CurrentVersion => "0.1.0";
    public int CheckCount { get; private set; }
    public Exception? Exception { get; set; }
    public Func<UpdateAvailability?>? OnCheck { get; set; }
    public Task<UpdateAvailability?>? PendingCheck { get; set; }

    public Task<UpdateAvailability?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        CheckCount++;
        if (Exception is not null)
        {
            return Task.FromException<UpdateAvailability?>(Exception);
        }

        if (PendingCheck is not null)
        {
            return PendingCheck;
        }

        return Task.FromResult(OnCheck?.Invoke());
    }

    public Task DownloadUpdatesAsync(
        UpdateAvailability update,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(100);
        return Task.CompletedTask;
    }

    public void ApplyUpdatesAndRestart(UpdateAvailability update)
    {
    }
}
