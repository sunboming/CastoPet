namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> PlatformPersistenceTestCases { get; } =
    [
        new("Invalid settings file falls back to defaults", InvalidSettingsFallsBackToDefaults),
        new("Logging writes a dated log file", LoggingWritesDatedLogFile),
        new("Logging rotates bounded archive files", LoggingRotatesBoundedArchiveFiles),
        new("Bottom-right placement uses work area margin", BottomRightPlacementUsesWorkAreaMargin),
        new("Startup value name is CastoPet", StartupValueNameIsCastoPet),
        new("Startup service accepts product registration identity", StartupServiceAcceptsProductRegistrationIdentity),
        new("Startup registration matches current executable path", StartupRegistrationMatchesCurrentExecutablePath),
        new("Project does not keep template MainWindow", ProjectDoesNotKeepTemplateMainWindow),
        new("Single instance rejects a second owner", SingleInstanceRejectsSecondOwner),
        new("Application composes the current product identity", ApplicationComposesTheCurrentProductIdentity),
        new("Single instance restore signal reaches primary", SingleInstanceRestoreSignalReachesPrimary),
        new("Runtime position starts at default", RuntimePositionStartsAtDefault),
        new("Runtime position tracks drag for current run only", RuntimePositionTracksDragForCurrentRunOnly),
        new("Show restore keeps hidden position but resets visible position", ShowRestoreKeepsHiddenPositionButResetsVisiblePosition),
    ];
}
