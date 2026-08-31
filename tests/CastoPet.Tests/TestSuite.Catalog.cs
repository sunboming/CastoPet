namespace CastoPet.Tests;

internal static partial class TestSuite
{
    internal static IReadOnlyList<TestCase> Tests { get; } =
    [
        .. GetRepositoryArchitectureTestCases(),
        .. GetUpdateTestCases(),
        .. GetReleaseBasicsTestCases(),
    ];
}
