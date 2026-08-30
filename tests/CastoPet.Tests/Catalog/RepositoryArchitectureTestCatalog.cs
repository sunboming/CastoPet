namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> GetRepositoryArchitectureTestCases() =>
    [
        new("Project pins semantic version and Velopack", ProjectPinsSemanticVersionAndVelopack),
        new("Application defines packaged icon", ApplicationDefinesPackagedIcon),
        new("Application surfaces share one icon", ApplicationSurfacesShareOneIcon),
        new("Tray service disposes owned menu resources", TrayServiceDisposesOwnedMenuResources),
        new("Release uses menus without a settings window", ReleaseUsesMenusWithoutSettingsWindow),
        new("Continuous integration builds both configurations", ContinuousIntegrationBuildsBothConfigurations),
        new("Release contains only basic desktop pet features", ReleaseContainsOnlyBasicDesktopPetFeatures),
        new("Packaging script builds a traceable installer", PackagingScriptBuildsTraceableInstaller),
        new("Packaging workflow produces manual artifacts without publishing", PackagingWorkflowProducesManualArtifactsWithoutPublishing),
        new("Release script creates a draft in the source repository", ReleaseScriptCreatesDraftInSourceRepository),
        new("Repository ignores local working assets", RepositoryIgnoresLocalWorkingAssets),
        new("Repository keeps authoring artwork outside source", RepositoryKeepsAuthoringArtworkOutsideSource),
        new("Production code is organized by architecture", ProductionCodeIsOrganizedByArchitecture),
        new("Input reactive feature is fully removed", InputReactiveFeatureIsFullyRemoved),
        new("Architecture dependencies point inward", ArchitectureDependenciesPointInward),
        new("Velopack runs at the application entry point", VelopackRunsAtTheApplicationEntryPoint),
        new("Update source points to the public releases repository", UpdateSourcePointsToThePublicReleasesRepository),
    ];
}
