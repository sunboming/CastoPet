namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> SettingsPresentationTestCases { get; } =
    [
        new("Settings window exposes crash and update actions", SettingsWindowExposesCrashAndUpdateActions),
        new("Pet window settings snapshot copies runtime flags", PetWindowSettingsSnapshotCopiesRuntimeFlags),
    ];
}
