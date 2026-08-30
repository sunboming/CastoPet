namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> GetReleaseBasicsTestCases() =>
    [
        new("Release settings expose only basic options", ReleaseSettingsExposeOnlyBasicOptions),
        new("Release uses one public product identity", ReleaseUsesOnePublicProductIdentity),
        new("Built-in skin provides idle and blink", BuiltInSkinProvidesIdleAndBlink),
        new("Maintenance menu commands use shared callbacks", MaintenanceMenuCommandsUseSharedCallbacks),
        new("Pet window contains only basic interaction entry points", PetWindowContainsOnlyBasicInteractionEntryPoints),
        new("Crash reports do not expose obsolete edition", CrashReportsDoNotExposeObsoleteEdition),
    ];
}
