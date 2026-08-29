namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> SettingsTestCases { get; } =
    [
        new("Default settings match MVP defaults", DefaultSettingsMatchMvpDefaults),
        new("Default active movement is disabled", DefaultActiveMovementIsDisabled),
        new("Default push cursor is disabled", DefaultPushCursorIsDisabled),
        new("Default theme follows the system", DefaultThemeFollowsTheSystem),
        new("Settings round trip as JSON", SettingsRoundTripAsJson),
        new("Settings round trip includes active movement", SettingsRoundTripIncludesActiveMovement),
        new("Settings round trip includes push cursor", SettingsRoundTripIncludesPushCursor),
        new("Settings round trip includes skin manifest path", SettingsRoundTripIncludesSkinManifestPath),
        new("Settings round trip includes theme mode", SettingsRoundTripIncludesThemeMode),
        new("Settings save is atomic and versioned", SettingsSaveIsAtomicAndVersioned),
        new("Settings load restores the last valid backup", SettingsLoadRestoresTheLastValidBackup),
        new("Settings load migrates legacy schema", SettingsLoadMigratesLegacySchema),
        new("Settings transaction rolls back failed persistence", SettingsTransactionRollsBackFailedPersistence),
        new("App paths include local crash reports", AppPathsIncludeLocalCrashReports),
        new("Product identities isolate stable and preview", ProductIdentitiesIsolateStableAndPreview),
        new("App paths follow product identity", AppPathsFollowProductIdentity),
        new("Preview data migration copies user configuration once", PreviewDataMigrationCopiesUserConfigurationOnce),
        new("Settings round trip includes crash and update state", SettingsRoundTripIncludesCrashAndUpdateState),
        new("Settings clone includes crash and update state", SettingsCloneIncludesCrashAndUpdateState),
        new("Settings clone includes theme mode", SettingsCloneIncludesThemeMode),
        new("Theme mode resolves system preference", ThemeModeResolvesSystemPreference),
        new("Settings theme palette defines light and dark contrast", SettingsThemePaletteDefinesLightAndDarkContrast),
        new("Settings theme palette replaces frozen brushes", SettingsThemePaletteReplacesFrozenBrushes),
        new("Windows system theme reader handles app preference", WindowsSystemThemeReaderHandlesAppPreference),
        new("Settings backdrop targets supported Windows versions", SettingsBackdropTargetsSupportedWindowsVersions),
    ];
}
