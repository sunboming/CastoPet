namespace CastoPet.Tests;

internal static partial class TestSuite
{
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

    static void RadialWheelOverlayStaysCenteredOnInvocationPoint()
    {
        var centered = RadialWheelOverlayPlacement.Calculate(640, 420, 436, 436);
        Assert.Equal(640d, centered.CenterX, "The interaction center should equal the invocation X coordinate.");
        Assert.Equal(420d, centered.CenterY, "The interaction center should equal the invocation Y coordinate.");
        Assert.Equal(422d, centered.Left, "The overlay should extend equally to the left of the invocation point.");
        Assert.Equal(202d, centered.Top, "The overlay should extend equally above the invocation point.");

        var nearBottomRight = RadialWheelOverlayPlacement.Calculate(1910, 1070, 436, 436);
        Assert.Equal(1910d, nearBottomRight.CenterX, "Screen-edge placement must not move the interaction center horizontally.");
        Assert.Equal(1070d, nearBottomRight.CenterY, "Screen-edge placement must not move the interaction center vertically.");
        Assert.Equal(1692d, nearBottomRight.Left, "Screen-edge placement should remain centered instead of clamping the wheel inward.");
        Assert.Equal(852d, nearBottomRight.Top, "Screen-edge placement should remain centered instead of clamping the wheel inward.");
    }

    static void RadialWheelPopupOverridesWpfEdgeRepositioning()
    {
        var devicePlacement = RadialWheelPopupPosition.Calculate(1910, 1070, 436, 436);
        Assert.Equal(1692, devicePlacement.Left, "The native popup should be centered horizontally on the device invocation point.");
        Assert.Equal(852, devicePlacement.Top, "The native popup should be centered vertically on the device invocation point.");

        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml"));
        Assert.Contains(xaml, "Opened=\"OnRadialWheelOverlayOpened\"", "The wheel should correct its native position after WPF opens the popup.");
        Assert.Contains(source, "WindowsPopupPositioner.TryCenterAt", "The opened popup should bypass WPF edge flipping through its native window position.");
    }

    static void RadialWheelStyleKeepsReadableRingHierarchy()
    {
        var first = RadialWheelStyle.GetNormalFill(RadialWheelRing.First, isEnabled: true);
        var second = RadialWheelStyle.GetNormalFill(RadialWheelRing.Second, isEnabled: true);
        var firstDisabled = RadialWheelStyle.GetNormalFill(RadialWheelRing.First, isEnabled: false);
        var secondDisabled = RadialWheelStyle.GetNormalFill(RadialWheelRing.Second, isEnabled: false);

        Assert.Equal((byte)218, first.Alpha, "First-ring glass should stay readable over the desktop.");
        Assert.Equal((byte)210, second.Alpha, "Second-ring glass should remain slightly lighter.");
        Assert.Equal((byte)145, firstDisabled.Alpha, "Disabled first-ring glass should remain subdued.");
        Assert.Equal((byte)136, secondDisabled.Alpha, "Disabled second-ring glass should remain subdued.");
        Assert.True(first.Red >= 225 && first.Green >= 210 && first.Blue >= 240, "The first ring should use a pale purple-white base.");
        Assert.True(second.Red >= 220 && second.Green >= 200 && second.Blue >= 238, "The second ring should use a distinct pale lavender base.");
        Assert.False(first.Equals(second), "The two normal ring fills should remain visually distinct.");
        Assert.True(RadialWheelStyle.SelectedStroke.Alpha > RadialWheelStyle.NormalStroke.Alpha, "Selection should rely on a clearer outline instead of a different fill.");
        Assert.True(RadialWheelStyle.SelectedStrokeThickness >= 2d, "Selection should have a clear outline-only treatment.");
        Assert.Equal(0d, RadialWheelStyle.SectorGapRadians, "Adjacent sectors should meet without transparent gaps.");
        Assert.True(RadialWheelStyle.NormalStrokeThickness >= 1d, "Sector boundaries should remain visible on pale glass.");
    }

    static void ShortcutWheelLoadsShellIcons()
    {
        using var temp = TempDirectory.Create();
        var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
        File.WriteAllText(filePath, "icon fixture");
        var workspace = FindWorkspaceRoot();
        var steamIconPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "AppIcon.ico");

        var fileIcon = ShortcutIconService.TryLoadSmallIcon(
            new ShortcutDefinition("file", "Notes", ShortcutType.File, filePath, "", null, 0));
        var webIcon = ShortcutIconService.TryLoadSmallIcon(
            new ShortcutDefinition("web", "Website", ShortcutType.WebUrl, "https://example.com", "", null, 1));
        var steamIcon = ShortcutIconService.TryLoadSmallIcon(
            new ShortcutDefinition("steam", "Steam", ShortcutType.SteamGame, "steam://rungameid/3419430", "", null, 2)
            {
                IconPath = steamIconPath,
            });

        Assert.True(fileIcon is not null, "Existing file shortcuts should expose a shell icon.");
        Assert.True(webIcon is not null, "Web shortcuts should expose the registered .url shell icon.");
        Assert.True(steamIcon is not null, "Steam shortcuts should load their persisted game icon.");

        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        Assert.Contains(source, "LoadShortcutWheelIcon", "Second-level shortcut items should resolve their icons.");
        Assert.Contains(source, "WpfControls.Image", "Shortcut wheel content should render icon images.");
        Assert.Contains(source, "visual.Content", "Selection scaling should apply to the combined icon-and-name content.");
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

    static void StablePointerGesturesKeepTraditionalContextMenu()
    {
        var classifier = new PetPointerGestureClassifier(
            6,
            6,
            14,
            TimeSpan.FromMilliseconds(400),
            radialWheelEnabled: false);
        var now = DateTimeOffset.Parse("2026-07-17T08:00:00Z");

        classifier.Press(PetPointerButton.Right, 50, 50, now);
        Assert.Equal(PetPointerIntent.None, classifier.Move(100, 50, now), "Stable right movement should not commit the radial wheel.");
        Assert.Equal(PetPointerIntent.None, classifier.UpdateHold(now.AddSeconds(1)), "Stable right hold should not commit the radial wheel.");
        Assert.Equal(PetPointerIntent.ContextMenu, classifier.Release(PetPointerButton.Right, 100, 50, now.AddSeconds(1)), "Stable right release should open the traditional menu.");
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

    static void InteractionCoordinatorPreservesShortClickIntent()
    {
        var coordinator = new PetInteractionCoordinator(
            CreateWheelCatalog(1),
            new PetPointerGestureClassifier(6, 6, 14, WheelCatalog.HoldDelay));
        var now = DateTimeOffset.UtcNow;

        coordinator.PressPointer(PetPointerButton.Right, 40, 40, now);
        var intent = coordinator.ReleasePointer(PetPointerButton.Right, 40, 40, now.AddMilliseconds(100));

        Assert.Equal(PetPointerIntent.ContextMenu, intent, "A right short click should remain a traditional menu gesture.");
        Assert.Equal(PetPointerGestureState.Idle, coordinator.PointerState, "Released short clicks should return to idle.");
    }

    static void InteractionCoordinatorOwnsWheelLifecycle()
    {
        var coordinator = new PetInteractionCoordinator(
            CreateWheelCatalog(1),
            new PetPointerGestureClassifier(6, 6, 14, WheelCatalog.HoldDelay));
        var now = DateTimeOffset.UtcNow;
        coordinator.PressPointer(PetPointerButton.Right, 0, 0, now);

        var intent = coordinator.MovePointer(20, 0, now.AddMilliseconds(20));
        Assert.Equal(PetPointerIntent.RadialWheel, intent, "A radial movement should commit the wheel intent.");
        Assert.True(coordinator.TryOpenRadialWheel(now.AddMilliseconds(20)), "A non-empty catalog should open the wheel.");
        Assert.True(coordinator.IsRadialWheelOpen, "Coordinator should own the visible wheel state.");
        Assert.True(coordinator.RadialWheel.IsOpen, "Coordinator should open the selection controller at the same time.");

        coordinator.UpdateCatalog(new WheelCatalog(Array.Empty<WheelCategory>()));
        Assert.False(coordinator.IsRadialWheelOpen, "Replacing the catalog should close the visible wheel.");
        Assert.False(coordinator.RadialWheel.IsOpen, "Replacing the catalog should close the selection controller.");
        Assert.Equal(PetPointerGestureState.Idle, coordinator.PointerState, "Replacing the catalog should cancel pending pointer state.");
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
        var compactSource = string.Concat(source.Where(character => !char.IsWhiteSpace(character)));

        Assert.Equal(1, source.Split("new ShortcutService(_paths, _logger)", StringSplitOptions.None).Length - 1, "App startup should construct exactly one shortcut service from shared paths and logging.");
        Assert.Equal(1, source.Split("_shortcutService.Load();", StringSplitOptions.None).Length - 1, "App startup should load the shared shortcut service exactly once.");
        Assert.Contains(source, "new WheelCatalogService(skin.Expressions, _shortcutService)", "The live catalog should observe the shared shortcut service.");
        Assert.Equal(1, source.Split("new ShortcutDropHandler(_shortcutService)", StringSplitOptions.None).Length - 1, "App startup should construct one shared drop handler.");
        Assert.Equal(1, source.Split("new ShortcutLauncher(_logger)", StringSplitOptions.None).Length - 1, "App startup should construct one shared launcher.");
        Assert.Contains(compactSource, "newPetWindow(assets,_logger,_wheelCatalogService,_shortcutService,_shortcutDropHandler,_shortcutLauncher,_features)", "The production window should receive the shared service graph and product profile.");
        Assert.Contains(source, "_wheelCatalogService?.Dispose();", "Application shutdown should release the catalog subscription.");
    }

    static void PetWindowFollowsLiveWheelCatalogSnapshots()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));

        Assert.Contains(source, "WheelCatalogService wheelCatalogService", "PetWindow should receive the live catalog service instead of a frozen snapshot.");
        Assert.Contains(source, "_wheelCatalogService.Changed += OnWheelCatalogChanged;", "PetWindow should observe live catalog changes.");
        Assert.Contains(source, "Dispatcher.InvokeAsync(RefreshWheelCatalog)", "Catalog refresh should be marshalled to the window dispatcher.");
        Assert.Contains(source, "_interactions.UpdateCatalog(_wheelCatalogService.Current);", "A refresh should install the latest catalog snapshot through the interaction coordinator.");
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

    static void RadialWheelSecondRingStaysWithCategoryDirection()
    {
        var leftArc = RadialWheelArcLayout.CreateSecondRingArc(1, 2, 8);
        Assert.Equal(Math.PI, leftArc.StartAngle, "The left category should start its submenu at the bottom-facing boundary.");
        Assert.Equal(Math.PI, leftArc.SweepAngle, "Eight submenu items should occupy one same-side semicircle.");

        var leftSelection = RadialWheelSelector.GetSelection(-170, 0, 2, 8, selectedCategoryIndex: 1);
        Assert.Equal(RadialWheelRing.Second, leftSelection.Ring, "The left outer ring should remain interactive for a left category.");
        Assert.True(leftSelection.SectorIndex >= 0, "The left outer ring should resolve a submenu item.");

        var oppositeSelection = RadialWheelSelector.GetSelection(170, 0, 2, 8, selectedCategoryIndex: 1);
        Assert.Equal(RadialWheelRing.Second, oppositeSelection.Ring, "The opposite side should remain inside the wheel tolerance.");
        Assert.Equal(-1, oppositeSelection.SectorIndex, "The opposite side must not select a submenu item.");

        var controller = new RadialWheelController(CreateWheelCatalog(8));
        var now = DateTimeOffset.UtcNow;
        controller.Open(now);
        controller.UpdatePointer(-80, 0, now);
        controller.UpdatePointer(-80, 0, now + WheelCatalog.CategoryDwellDelay);
        controller.UpdatePointer(170, 0, now + WheelCatalog.CategoryDwellDelay + TimeSpan.FromMilliseconds(1));
        Assert.True(controller.IsOpen, "Crossing the opposite outer side should not abruptly close the wheel.");
        Assert.Equal(-1, controller.SelectedSecondLevelIndex, "The opposite side should clear submenu selection.");
    }

    static void RadialWheelFastOutwardMotionOpensSecondLevel()
    {
        var controller = new RadialWheelController(CreateWheelCatalog(8));
        var now = DateTimeOffset.UtcNow;
        controller.Open(now);

        controller.UpdatePointer(-170, 0, now);
        Assert.Equal(1, controller.SelectedCategoryIndex, "A direct jump to the left outer ring should infer the left category.");
        Assert.False(controller.IsSecondLevelOpen, "The inferred category should still respect dwell timing.");

        controller.UpdatePointer(-170, 0, now + WheelCatalog.CategoryDwellDelay);
        Assert.True(controller.IsSecondLevelOpen, "Holding in the outer ring after a fast jump should open level two.");
        Assert.Equal(1, controller.SelectedCategoryIndex, "The inferred category should remain selected after opening.");
        Assert.True(controller.SelectedSecondLevelIndex >= 0, "The current outer pointer should select an item immediately after opening.");
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

        controller.UpdatePointer(220, 0, now + WheelCatalog.CategoryDwellDelay + TimeSpan.FromMilliseconds(1));

        Assert.True(controller.IsOpen, "A slight overshoot should not close the wheel.");
        Assert.True(controller.SelectedSecondLevelIndex >= 0, "The overshoot area should preserve same-side outer-ring selection.");

        controller.UpdatePointer(239, 0, now + WheelCatalog.CategoryDwellDelay + TimeSpan.FromMilliseconds(2));
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
}
