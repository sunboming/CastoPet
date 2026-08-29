namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> RepositoryArchitectureTestCases { get; } =
    [
        new("Project pins semantic version and Velopack", ProjectPinsSemanticVersionAndVelopack),
        new("Application defines packaged icon", ApplicationDefinesPackagedIcon),
        new("Application surfaces share one icon", ApplicationSurfacesShareOneIcon),
        new("Tray service disposes owned menu resources", TrayServiceDisposesOwnedMenuResources),
        new("Settings window avoids a duplicate taskbar entry", SettingsWindowAvoidsDuplicateTaskbarEntry),
        new("Continuous integration builds both configurations", ContinuousIntegrationBuildsBothConfigurations),
        new("Project supports stable and preview resource profiles", ProjectSupportsStableAndPreviewResourceProfiles),
        new("Packaging script builds traceable edition-specific installers", PackagingScriptBuildsTraceableEditionSpecificInstallers),
        new("Packaging workflow produces manual artifacts without publishing", PackagingWorkflowProducesManualArtifactsWithoutPublishing),
        new("Repository ignores local working assets", RepositoryIgnoresLocalWorkingAssets),
        new("Repository keeps authoring artwork outside source", RepositoryKeepsAuthoringArtworkOutsideSource),
        new("Production code is organized by architecture", ProductionCodeIsOrganizedByArchitecture),
        new("Input reactive feature is fully removed", InputReactiveFeatureIsFullyRemoved),
        new("Architecture dependencies point inward", ArchitectureDependenciesPointInward),
        new("Velopack runs at the application entry point", VelopackRunsAtTheApplicationEntryPoint),
        new("Update source points to the public releases repository", UpdateSourcePointsToThePublicReleasesRepository),
    ];
}
