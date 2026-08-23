namespace CastoPet.Tests;

internal static partial class TestSuite
{
    internal static IReadOnlyList<TestCase> Tests { get; } =
    [
        .. SettingsTestCases,
        .. CrashDiagnosticsTestCases,
        .. UpdateTestCases,
        .. RepositoryArchitectureTestCases,
        .. SettingsPresentationTestCases,
        .. PlatformPersistenceTestCases,
        .. BuiltInSkinTestCases,
        .. SkinManifestTestCases,
        .. SkinSelectionTestCases,
        .. AssetServiceTestCases,
        .. PackagedAnimationTestCases,
        .. WheelInteractionTestCases,
        .. ShortcutTestCases,
        .. WindowAndSettingsTestCases,
        .. InputMovementAnimationTestCases,
    ];
}
