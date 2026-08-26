namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void PetWindowDisablesMovementTurnTransitions()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        Assert.Contains(source, "MovementTurnTransitionsEnabled = false", "Movement transitions should be explicitly disabled without removing skin resources.");
        var loader = ExtractSourceSection(source, "private IReadOnlyList<ImageSource> GetTurnFrames(", "private IReadOnlyList<ImageSource> TryLoadRequiredMoveFrames(");
        Assert.Contains(loader, "if (!MovementTurnTransitionsEnabled)", "Disabled transitions should skip loading turn images.");
        Assert.True(loader.IndexOf("Array.Empty<ImageSource>()", StringComparison.Ordinal) < loader.IndexOf("_assets.LoadTurnLeftFrames()", StringComparison.Ordinal), "The disabled path should return before loading turn assets.");
        var facing = ExtractSourceSection(source, "private bool EnsureMovementFacing(", "private void AdvanceTurnFrame(");
        Assert.Contains(facing, "GetMoveFrames(direction)", "Immediate facing should display the requested walking animation without waiting for a distance tick.");
    }

    static void MenuCommandsPreserveBehaviorThroughApplicationBoundaries()
    {
        var settings = AppSettings.Default;
        settings.Topmost = false;
        settings.StartWithWindows = false;
        var target = new FakePetCommandTarget();
        var store = new FakeSettingsStore();
        var startup = new FakeStartupRegistration();
        var logger = new FakeApplicationLogger();
        var notifications = new FakeUserNotificationService();
        var shutdown = new FakeApplicationShutdown();
        var commands = new MenuCommandService(
            target,
            settings,
            store,
            startup,
            logger,
            notifications,
            shutdown,
            "CastoPet.exe");

        commands.ToggleTopmost();
        Assert.True(settings.Topmost, "Successful menu changes should mutate settings.");
        Assert.Equal(1, store.SaveCount, "Successful menu changes should persist once.");
        Assert.Equal(1, target.ApplyCount, "Successful visual settings should apply to the pet once.");

        store.SaveResult = false;
        commands.ToggleTopmost();
        Assert.True(settings.Topmost, "Failed persistence should restore the prior setting value.");
        Assert.Equal(1, notifications.WarningCount, "Failed persistence should retain the warning behavior.");

        startup.SetResult = false;
        commands.ToggleStartWithWindows();
        Assert.False(settings.StartWithWindows, "Failed startup registration should not mutate settings.");
        Assert.Equal(2, notifications.WarningCount, "Failed startup registration should retain the warning behavior.");

        commands.Exit();
        Assert.Equal(1, shutdown.Count, "Exit should request application shutdown once.");
        Assert.True(logger.Messages.Contains("CastoPet exiting."), "Exit should retain its log entry.");
    }

    static void PetWindowDefinesTwoLevelRadialOverlayAndDropSurface()
    {
        var workspace = FindWorkspaceRoot();
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml"));

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
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml"));

        Assert.Contains(source, "RadialWheelStyle.GetNormalFill", "Initial and restored fills should use the shared style contract.");
        Assert.Contains(source, "visual.Ring", "Selection refresh should restore the visual's original ring style.");
        Assert.Contains(source, "RadialWheelStyle.SectorGapRadians", "Sector geometry should use the refined divider gap.");
        Assert.Contains(source, "TextFormattingMode.Display", "Small wheel labels should use crisp display-oriented text formatting.");
        Assert.Contains(source, "TextRenderingMode.Grayscale", "Transparent wheel labels should avoid blurry ClearType color fringing.");
        Assert.Contains(xaml, "x:Name=\"SecondRingBoundary\"", "The outer ring should expose a continuous boundary.");
        Assert.Contains(xaml, "Color=\"#F2FFFFFF\"", "The center should use a bright frosted-glass highlight.");
        Assert.Contains(xaml, "x:Key=\"WheelBoundaryBrush\"", "Ring boundaries should share one coherent purple treatment.");
    }

    static void PetWindowRoutesClassifiedPointerGestures()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));

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
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml"));

        Assert.Contains(xaml, "RadialWheelHoldOverlay", "Right hold should have a dedicated non-interactive progress overlay.");
        Assert.Contains(xaml, "RadialWheelHoldArc", "Hold feedback should expose an updateable circular arc.");
        Assert.Contains(source, "GetRightHoldProgress", "Hold feedback should use classifier timing.");
        Assert.Contains(source, "CompositionTarget.Rendering += OnRadialWheelHoldRendering", "Hold feedback should update on display composition frames.");
        Assert.Contains(source, "CompositionTarget.Rendering -= OnRadialWheelHoldRendering", "Hold feedback should release its composition-frame subscription.");
        Assert.False(xaml.Contains("DropShadowEffect", StringComparison.Ordinal), "The changing hold arc should not invalidate a real-time blur effect every frame.");
        Assert.Contains(source, "LoadPettingFrames", "PetWindow should load optional skin petting frames.");
        Assert.Contains(source, "AdvancePettingFrame", "Petting should play as a one-shot frame sequence.");
        Assert.Contains(source, "_animationController.IsPetting", "Passive runtime modes should gate on centralized petting playback state.");
    }

    static void PetWindowAppliesPerFrameActionDurations()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));

        Assert.Contains(source, "PetFrameTiming.GetDuration(_idleAction", "Idle playback should schedule the currently displayed frame duration.");
        Assert.Contains(source, "PetFrameTiming.GetDuration(_blinkAction", "Blink playback should schedule the currently displayed frame duration.");
        Assert.Contains(source, "PetFrameTiming.GetDuration(_pettingAction", "Petting playback should schedule the currently displayed frame duration.");
        Assert.Contains(source, "GetExpressionTransitionFrameDuration", "Generic expression transitions should schedule each displayed frame independently.");
        Assert.Contains(source, "PetFrameTiming.GetTotalDuration", "Petting compression should follow the authored irregular sequence duration.");
    }

    static void PetWindowSchedulesDirectionalTurnsAtRenderPriority()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));

        Assert.Contains(
            source,
            "new DispatcherTimer(DispatcherPriority.Render) { Interval = DefaultTurnFrameInterval }",
            "Directional turn frames should not be starved behind continuous composition rendering.");
    }

    static void PetWindowCompletesActiveMovementAfterOneCursorPush()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        const string advanceSignature = "private void AdvanceActiveMovement(TimeSpan renderingTime)";
        var advance = ExtractSourceSection(source, advanceSignature, "\n    private ");

        Assert.Contains(source, "private bool TryPushCursor", "Cursor pushing should report whether the single push happened.");
        Assert.Contains(advance, "if (TryPushCursor(renderingTime", "Active movement should branch immediately after the first cursor push.");
        Assert.Contains(advance, "_movementController.CompleteTarget(DateTime.UtcNow);", "A successful cursor push should complete the movement target.");
        Assert.Contains(advance, "FinishDirectionalMovement();", "A successful cursor push should begin returning to front-facing idle.");
        Assert.Contains(advance, "StopActiveMovementRendering();", "A successful cursor push should stop further movement and cursor nudges.");
        Assert.Contains(source, "_cursorPushGate.CompletePush();", "A successful cursor push should latch the current proximity session.");
        Assert.Contains(source, "_cursorPushGate.ObserveCursorDistance(cursorDistance", "Movement probing should release the latch only after the cursor exits.");
        Assert.Contains(source, "_pushCursorEnabled && !_cursorPushGate.AllowsPush", "Movement probing should not restart while the completed push remains latched.");
        Assert.False(source.Contains("_cursorPushOwnsMovementTarget", StringComparison.Ordinal), "One-shot pushing should not retain continuous target ownership state.");
    }

    static void PetWindowReleasesRuntimeResourcesOnClose()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        const string signature = "private void ShutdownRuntimeResources()";
        var cleanup = ExtractSourceSection(source, signature, "\n    private ");

        foreach (var timer in new[]
        {
            "_idleFrameTimer", "_blinkScheduleTimer", "_blinkFrameTimer", "_pettingFrameTimer",
            "_dragRestoreTimer", "_radialWheelPointerProbeTimer", "_temporaryExpressionTimer",
            "_expressionTransitionFrameTimer", "_activeMovementProbeTimer", "_inputReactiveRenderTimer",
        })
        {
            Assert.Contains(cleanup, $"{timer}.Stop();", $"Runtime cleanup should stop {timer}.");
        }

        Assert.Contains(cleanup, "StopActiveMovementRendering();", "Runtime cleanup should detach active movement rendering.");
        Assert.Contains(cleanup, "StopRadialWheelHoldRendering();", "Runtime cleanup should detach hold-progress rendering.");
        Assert.Contains(cleanup, "RadialWheelOverlay.IsOpen = false;", "Runtime cleanup should close the wheel popup.");
        Assert.Contains(cleanup, "RadialWheelHoldOverlay.IsOpen = false;", "Runtime cleanup should close the hold popup.");
        Assert.Contains(cleanup, "_inputHookService.Dispose();", "Runtime cleanup should release native input hooks.");
        Assert.Contains(cleanup, "_runtimeResourcesReleased", "Runtime cleanup should be idempotent.");
    }

    static void PetWindowDetachesContextMenuSubscriptions()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));

        Assert.Contains(source, "Action? _menuSettingsChangedHandler", "PetWindow should retain the context-menu settings handler for unsubscription.");
        Assert.Contains(source, "DetachContextMenuSubscriptions();", "Context-menu subscriptions should be detached during replacement and shutdown.");
        Assert.Contains(source, "_menuCommands.SettingsChanged -= _menuSettingsChangedHandler", "The shared command service must not retain a closed pet menu.");
        Assert.False(source.Contains("commands.SettingsChanged += () => RefreshContextMenuChecks(menu);", StringComparison.Ordinal), "The context menu must not use an anonymous external subscription.");
    }

    static void PetWindowRoutesGenericRadialActions()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));

        Assert.Contains(source, "RadialWheelController", "PetWindow should delegate wheel state to the generic controller.");
        Assert.Contains(source, "DateTimeOffset.UtcNow", "Category dwell should be driven by explicit timestamps.");
        Assert.Contains(source, "WheelReleaseKind.PageChanged", "Page controls should keep the wheel open and refresh its page.");
        Assert.Contains(source, "WheelActionType.Expression", "Expression actions should be dispatched by generic action type.");
        Assert.Contains(source, "WheelActionType.Shortcut", "Shortcut actions should be dispatched by generic action type.");
        Assert.Contains(source, "_expressionAssetCache.Get(expressionId)", "Expression actions should resolve assets lazily by expression ID.");
        Assert.Contains(source, "_shortcutLauncher.Launch", "Shortcut actions should use the injected launcher.");
        Assert.Contains(source, "WpfInput.Key.Escape", "Escape should cancel an open radial wheel.");
    }

    static void PetWindowExtractsNeutralShortcutDropData()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        var readerSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Infrastructure", "Shortcuts", "ShortcutDropDataReader.cs"));

        Assert.Contains(readerSource, "WpfDataFormats.FileDrop", "WPF file-drop data should be extracted into paths.");
        Assert.Contains(readerSource, "WpfDataFormats.UnicodeText", "Unicode text drops should be extracted into neutral strings.");
        Assert.Contains(readerSource, "UniformResourceLocator", "Browser URL clipboard formats should be recognized.");
        Assert.Contains(source, "_shortcutDrops.AddDroppedItems", "Only neutral path and text lists should cross into Core.");
        Assert.Contains(source, "DragDropEffects.Link", "Drop feedback should communicate that source files remain untouched.");
        Assert.False(source.Contains("File.Move(", StringComparison.Ordinal), "PetWindow must never move a dropped source file.");
    }

    static void PetWindowRetiresExpressionOnlyWheelIntegration()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        var selectorPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "Core", "Wheel", "ExpressionWheelSelector.cs");
        var catalogSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Core", "Wheel", "ExpressionWheelCatalog.cs"));

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
        var definitions = SettingCatalog.Create(
            AppSettings.Default,
            SettingActions.None,
            CastoPetFeatureProfile.Preview);

        Assert.Equal(
            "topmost,active-movement,click-through,push-cursor,input-reactive-mode,show-in-taskbar,start-with-windows",
            string.Join(',', definitions.Select(item => item.Id)),
            "The settings catalog should contain every boolean setting exactly once in group order.");
        Assert.Equal(
            "Behavior,Behavior,Interaction,Interaction,Interaction,System,System",
            string.Join(',', definitions.Select(item => item.Group)),
            "Settings should remain in stable behavior, interaction, and system groups.");
    }

    static void BuildFeatureProfilesDefineStableAndPreviewBoundaries()
    {
        var stable = CastoPetFeatureProfile.Stable;
        var preview = CastoPetFeatureProfile.Preview;

        Assert.Equal(CastoPetEdition.Stable, stable.Edition, "The stable profile should identify its edition.");
        Assert.False(stable.Petting, "Stable should not include left-click petting.");
        Assert.False(stable.RadialWheel, "Stable should not include the radial wheel.");
        Assert.False(stable.ShortcutLauncher, "Stable should not include shortcut launching.");
        Assert.False(stable.ActiveMovement, "Stable should not include autonomous movement.");
        Assert.False(stable.PushCursor, "Stable should not include cursor pushing.");
        Assert.False(stable.InputReactiveMode, "Stable should not include input-reactive mode.");
        Assert.False(stable.ExternalSkins, "Stable should use only the built-in skin.");

        Assert.Equal(CastoPetEdition.Preview, preview.Edition, "The preview profile should identify its edition.");
        Assert.True(preview.Petting && preview.RadialWheel && preview.ShortcutLauncher, "Preview should retain interaction experiments.");
        Assert.True(preview.ActiveMovement && preview.PushCursor && preview.InputReactiveMode, "Preview should retain movement and input experiments.");
        Assert.True(preview.ExternalSkins, "Preview should retain external skin loading.");
    }

    static void StableSettingCatalogExcludesPreviewBehavior()
    {
        var definitions = SettingCatalog.Create(
            AppSettings.Default,
            SettingActions.None,
            CastoPetFeatureProfile.Stable);

        Assert.Equal(
            "topmost,click-through,show-in-taskbar,start-with-windows",
            string.Join(',', definitions.Select(item => item.Id)),
            "Stable settings should expose only the supported basic desktop behavior.");
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
        var definitions = SettingCatalog.Create(
            settings,
            SettingActions.None,
            CastoPetFeatureProfile.Preview);
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
        var xamlPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains(xaml, "SettingsItemsHost", "The settings window should expose its catalog host.");
        Assert.Contains(xaml, "MiSans, Noto Sans SC, Microsoft YaHei UI", "The window should use the approved Chinese font stack.");
        Assert.Contains(xaml, "#8C7AA5", "Active controls should use dusty mist violet.");
        Assert.Contains(xaml, "#FAF9FC", "The main surface should use cool near-white.");
        Assert.False(xaml.Contains("#6F4AA8", StringComparison.Ordinal), "The old saturated purple should be removed.");
        Assert.Contains(xaml, "CloseButton", "The custom title bar should expose a close button.");
    }

    static void SettingsWindowSupportsThemeSwitchingAndBackdrop()
    {
        var workspace = FindWorkspaceRoot();
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml"));
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml.cs"));
        var commands = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Application", "Menus", "MenuCommandService.cs"));
        var backdrop = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Infrastructure", "Platform", "SettingsBackdropService.cs"));

        Assert.Contains(xaml, "AllowsTransparency=\"False\"", "Native backdrop windows should not use WPF layered transparency.");
        Assert.Contains(xaml, "SystemThemeButton", "Settings should expose a follow-system theme choice.");
        Assert.Contains(xaml, "LightThemeButton", "Settings should expose a light theme choice.");
        Assert.Contains(xaml, "DarkThemeButton", "Settings should expose a dark theme choice.");
        Assert.Contains(xaml, "ThemeMode_Checked", "Theme choices should apply immediately.");
        Assert.Contains(source, "ThemeModeResolver.Resolve", "Settings should resolve the persisted theme against Windows.");
        Assert.Contains(source, "WindowsSystemThemeReader.UsesDarkApps", "Follow-system mode should read the Windows app preference.");
        Assert.Contains(source, "SettingsThemePalette.Apply", "Settings should apply its centralized palette.");
        Assert.Contains(source, "SettingsBackdropService.TryApply", "Settings should request the supported native backdrop.");
        Assert.Contains(backdrop, "DwmExtendFrameIntoClientArea", "The native backdrop should extend through the custom client area.");
        Assert.Contains(commands, "SetThemeMode(AppThemeMode mode)", "Theme selection should persist through the shared settings command service.");
    }

    static void SettingsWindowCancelsUpdateWorkOnClose()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml.cs"));

        Assert.Contains(source, "CancellationTokenSource _lifetimeCancellation", "Each settings window should own cancellation for its asynchronous work.");
        Assert.Contains(source, "CheckAsync(manual: true, _lifetimeCancellation.Token)", "Manual update checks should observe window closure.");
        Assert.Contains(source, "DownloadUpdatesAsync(update, progress, _lifetimeCancellation.Token)", "Update downloads should observe window closure.");
        Assert.Contains(source, "_lifetimeCancellation.Cancel();", "Closing the settings window should cancel outstanding update work.");
        Assert.Contains(source, "if (_isClosed)", "Asynchronous continuations should guard against writing to a closed window.");
    }

    static void SettingsWindowExposesShortcutLauncherManagement()
    {
        var workspace = FindWorkspaceRoot();
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml"));

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
        var windowSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml.cs"));
        var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));
        var compactAppSource = string.Concat(appSource.Where(character => !char.IsWhiteSpace(character)));

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
        Assert.Contains(compactAppSource, "newSettingsWindow(commands,_crashReports,_updates,_shortcutService,_shortcutDropHandler,_shortcutLauncher,_features)", "App should pass the same composed shortcut dependencies and product profile to settings.");
    }

    static void SettingsShortcutListAcceptsSharedDropData()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var xaml = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml"));
        var settingsSource = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml.cs"));
        var petSource = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml.cs"));

        Assert.Contains(xaml, "AllowDrop=\"True\"", "The shortcut list should accept external drops.");
        Assert.Contains(xaml, "DragOver=\"ShortcutList_DragOver\"", "The shortcut list should classify incoming drag data.");
        Assert.Contains(xaml, "Drop=\"ShortcutList_Drop\"", "The shortcut list should add dropped items.");
        Assert.Contains(settingsSource, "ShortcutDropDataReader.ExtractPaths", "Settings should use the shared neutral path reader.");
        Assert.Contains(settingsSource, "_shortcutDropHandler.AddDroppedItems", "Settings drops should reuse shortcut safety and persistence rules.");
        Assert.Contains(petSource, "ShortcutDropDataReader.ExtractPaths", "Pet drops should use the same neutral path reader.");
    }

    static void DirectMenusExposeTheSettingsCommand()
    {
        Assert.Equal("设置", TrayService.SettingsText, "Direct menus should expose the settings window command.");
    }
}
