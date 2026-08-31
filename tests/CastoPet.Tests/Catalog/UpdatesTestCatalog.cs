namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> GetUpdateTestCases() =>
    [
        new("Update policy checks at most once per local day", UpdatePolicyChecksAtMostOncePerLocalDay),
        new("Manual update checks bypass the daily gate", ManualUpdateChecksBypassTheDailyGate),
        new("Update coordinator skips development builds", UpdateCoordinatorSkipsDevelopmentBuilds),
        new("Disabled update service stays disabled", DisabledUpdateServiceStaysDisabled),
        new("Update coordinator records automatic attempts before network", UpdateCoordinatorRecordsAutomaticAttemptsBeforeNetwork),
        new("Update coordinator maps network failures", UpdateCoordinatorMapsNetworkFailures),
        new("Update coordinator logs network failures", UpdateCoordinatorLogsNetworkFailures),
        new("Update coordinator rejects concurrent checks", UpdateCoordinatorRejectsConcurrentChecks),
        new("Update coordinator allows only one interactive workflow", UpdateCoordinatorAllowsOnlyOneInteractiveWorkflow),
        new("Test update source requires an explicit existing directory", TestUpdateSourceRequiresAnExplicitExistingDirectory),
    ];
}
