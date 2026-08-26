namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> WindowAndSettingsTestCases { get; } =
    [
        new("Pet window switches movement direction immediately", PetWindowSwitchesMovementDirectionImmediately),
        new("Pet window defines two-level radial overlay and drop surface", PetWindowDefinesTwoLevelRadialOverlayAndDropSurface),
        new("Pet window consumes centralized radial wheel styling", PetWindowConsumesCentralizedRadialWheelStyling),
        new("Pet window routes classified pointer gestures", PetWindowRoutesClassifiedPointerGestures),
        new("Pet window defines hold feedback and petting playback", PetWindowDefinesHoldFeedbackAndPettingPlayback),
        new("Pet window applies per-frame action durations", PetWindowAppliesPerFrameActionDurations),
        new("Pet window has no turn playback resources", PetWindowHasNoTurnPlaybackResources),
        new("Pet window completes active movement after one cursor push", PetWindowCompletesActiveMovementAfterOneCursorPush),
        new("Pet window releases runtime resources on close", PetWindowReleasesRuntimeResourcesOnClose),
        new("Pet window detaches context menu subscriptions", PetWindowDetachesContextMenuSubscriptions),
        new("Pet window routes generic radial actions", PetWindowRoutesGenericRadialActions),
        new("Pet window extracts neutral shortcut drop data", PetWindowExtractsNeutralShortcutDropData),
        new("Pet window retires expression-only wheel integration", PetWindowRetiresExpressionOnlyWheelIntegration),
        new("Setting catalog defines every boolean setting once", SettingCatalogDefinesEveryBooleanSettingOnce),
        new("Build feature profiles define stable and preview boundaries", BuildFeatureProfilesDefineStableAndPreviewBoundaries),
        new("Stable setting catalog excludes preview behavior", StableSettingCatalogExcludesPreviewBehavior),
        new("Setting catalog exposes only common direct menu settings", SettingCatalogExposesOnlyCommonDirectMenuSettings),
        new("Setting catalog reads shared settings live", SettingCatalogReadsSharedSettingsLive),
        new("Settings window service reuses the open window", SettingsWindowServiceReusesTheOpenWindow),
        new("Settings window service releases a closed window", SettingsWindowServiceReleasesAClosedWindow),
        new("Settings window defines the approved visual structure", SettingsWindowDefinesTheApprovedVisualStructure),
        new("Settings window supports theme switching and backdrop", SettingsWindowSupportsThemeSwitchingAndBackdrop),
        new("Settings window cancels update work on close", SettingsWindowCancelsUpdateWorkOnClose),
        new("Settings window exposes shortcut launcher management", SettingsWindowExposesShortcutLauncherManagement),
        new("Settings window shares shortcut services and live updates", SettingsWindowSharesShortcutServicesAndLiveUpdates),
        new("Settings shortcut list accepts shared drop data", SettingsShortcutListAcceptsSharedDropData),
        new("Direct menus expose the settings command", DirectMenusExposeTheSettingsCommand),
        new("Menu commands preserve behavior through application boundaries", MenuCommandsPreserveBehaviorThroughApplicationBoundaries),
    ];
}
