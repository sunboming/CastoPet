namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> GetReleaseBasicsTestCases() =>
    [
        new("Release settings expose only basic options", ReleaseSettingsExposeOnlyBasicOptions),
        new("Release uses one public product identity", ReleaseUsesOnePublicProductIdentity),
        new("Portable distribution keeps user data beside the application", PortableDistributionKeepsUserDataBesideTheApplication),
        new("Built-in skin provides idle and blink", BuiltInSkinProvidesIdleAndBlink),
        new("Maintenance menu commands use shared callbacks", MaintenanceMenuCommandsUseSharedCallbacks),
        new("Current update message includes installed version", CurrentUpdateMessageIncludesInstalledVersion),
        new("Pet window contains only basic interaction entry points", PetWindowContainsOnlyBasicInteractionEntryPoints),
        new("Crash reports do not expose obsolete edition", CrashReportsDoNotExposeObsoleteEdition),
        new("Release settings persist and recover from corruption", ReleaseSettingsPersistAndRecoverFromCorruption),
        new("Release crash reports sanitize and retain bounded history", ReleaseCrashReportsSanitizeAndRetainBoundedHistory),
        new("Release logging rotates bounded archives", ReleaseLoggingRotatesBoundedArchives),
        new("Release single instance rejects a second owner", ReleaseSingleInstanceRejectsASecondOwner),
        new("Release startup registration matches the current executable", ReleaseStartupRegistrationMatchesTheCurrentExecutable),
    ];
}
