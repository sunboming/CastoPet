namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void ProjectPinsSemanticVersionAndVelopack()
    {
        var workspace = FindWorkspaceRoot();
        var project = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj"));
        var sharedProperties = File.ReadAllText(System.IO.Path.Combine(workspace, "Directory.Build.props"));

        Assert.Contains(sharedProperties, "<VersionPrefix>0.1.0</VersionPrefix>", "The repository should have one explicit semantic version source.");
        Assert.False(project.Contains("<Version>", StringComparison.Ordinal), "The application project should inherit the central semantic version.");
        Assert.Contains(project, "<PackageReference Include=\"Velopack\" Version=\"1.2.0\"", "Velopack should be pinned to the verified stable version.");
    }

    static void ApplicationDefinesPackagedIcon()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var project = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        var iconPath = System.IO.Path.Combine(projectRoot, "Assets", "AppIcon.ico");

        Assert.Contains(project, @"<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>", "The Windows executable should embed the CastoPet icon.");
        Assert.True(File.Exists(iconPath), "The configured application icon should exist.");
        var icon = File.ReadAllBytes(iconPath);
        Assert.True(icon.Length > 6, "The application icon should contain an ICO directory.");
        Assert.True(icon[0] == 0 && icon[1] == 0 && icon[2] == 1 && icon[3] == 0, "The application icon should use the ICO signature.");
        var imageCount = icon[4] | icon[5] << 8;
        Assert.True(imageCount >= 4, "The application icon should contain multiple sizes for Windows shell surfaces.");
    }

    static void ApplicationSurfacesShareOneIcon()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var project = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        var petWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml"));
        var settingsWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml"));
        var crashWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "CrashNotificationWindow.xaml"));
        var trayService = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "TrayService.cs"));
        var iconService = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "ApplicationIconService.cs"));

        Assert.Contains(project, @"<Resource Include=""Assets\AppIcon.ico"" />", "The shared icon should be available as a WPF resource.");
        Assert.Contains(petWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "The pet taskbar surface should use the shared icon.");
        Assert.Contains(settingsWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "Settings should use the shared icon.");
        Assert.Contains(crashWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "Crash notifications should use the shared icon.");
        Assert.Contains(trayService, "ApplicationIconService.LoadTrayIcon()", "The notification-area icon should use the shared icon service.");
        Assert.False(trayService.Contains("SystemIcons.Application", StringComparison.Ordinal), "The notification area should not fall back to the generic Windows application icon.");
        Assert.Contains(iconService, "/CastoPet;component/Assets/AppIcon.ico", "The tray icon service should load the icon from the CastoPet assembly.");
        using var trayIcon = ApplicationIconService.LoadTrayIcon();
        Assert.True(trayIcon.Width > 0 && trayIcon.Height > 0, "The packaged icon should decode for the notification area at runtime.");
    }

    static void TrayServiceDisposesOwnedMenuResources()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Infrastructure", "Platform", "TrayService.cs"));

        Assert.Contains(source, "Forms.ContextMenuStrip _contextMenu", "TrayService should retain ownership of its native menu component.");
        Assert.Contains(source, "_notifyIcon.ContextMenuStrip = null;", "TrayService should detach the menu before disposing native components.");
        Assert.Contains(source, "_contextMenu.Dispose();", "TrayService should explicitly release its context menu and item handles.");
        Assert.Contains(source, "if (_disposed)", "TrayService disposal should be idempotent.");
    }

    static void SettingsWindowAvoidsDuplicateTaskbarEntry()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var settingsWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml"));
        var app = File.ReadAllText(System.IO.Path.Combine(projectRoot, "App.xaml.cs"));

        Assert.Contains(settingsWindow, "ShowInTaskbar=\"False\"", "Settings should remain an auxiliary window instead of creating a second taskbar button.");
        Assert.Contains(app, "Owner = _window", "Settings should be owned by the pet window for activation and lifetime behavior.");
    }

    static void ContinuousIntegrationBuildsBothConfigurations()
    {
        var workspace = FindWorkspaceRoot();
        var workflow = File.ReadAllText(System.IO.Path.Combine(workspace, ".github", "workflows", "build.yml"));

        Assert.Contains(workflow, "runs-on: windows-latest", "WPF CI should run on Windows.");
        Assert.Contains(workflow, "uses: actions/checkout@v6", "CI should use the current official checkout action.");
        Assert.Contains(workflow, "uses: actions/setup-dotnet@v5", "CI should use the current official .NET setup action.");
        Assert.Contains(workflow, "dotnet-version: 10.0.x", "CI should install the .NET 10 SDK.");
        Assert.Contains(workflow, "configuration: [Debug, Release]", "CI should cover both supported build configurations.");
        Assert.Contains(workflow, "dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj", "CI should execute the repository test harness.");
        Assert.Contains(workflow, "dotnet build CastoPet.sln", "CI should build the complete solution.");
        Assert.False(workflow.Contains("dotnet publish", StringComparison.OrdinalIgnoreCase), "Build CI should not publish release artifacts.");
    }

    static void ProjectSupportsStableAndPreviewResourceProfiles()
    {
        var workspace = FindWorkspaceRoot();
        var project = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj"));
        var workflow = File.ReadAllText(System.IO.Path.Combine(workspace, ".github", "workflows", "build.yml"));

        Assert.Contains(project, "<CastoPetEdition Condition=", "The app project should define a default edition.");
        Assert.Contains(project, "CASTOPET_STABLE", "Stable builds should expose one centralized compilation symbol.");
        Assert.Contains(project, "'$(CastoPetEdition)' == 'Stable'", "Stable resources should be selected through the edition property.");
        Assert.Contains(project, @"States\Idle\*.png", "Stable builds should package idle frames.");
        Assert.Contains(project, @"States\Blink\*.png", "Stable builds should package blink frames.");
        Assert.Contains(project, @"States\Castorice.Dragging.png", "Stable builds should retain the dragging visual.");
        Assert.Contains(project, @"Assets\Runtime\Castorice\**\*.png", "Preview builds should retain the complete runtime asset set.");
        Assert.Contains(workflow, "edition: [Preview, Stable]", "CI should verify both product editions.");
        Assert.Contains(workflow, "-p:CastoPetEdition=${{ matrix.edition }}", "CI should pass the edition explicitly to tests and builds.");
    }

    static void PackagingScriptBuildsTraceableEditionSpecificInstallers()
    {
        var workspace = FindWorkspaceRoot();
        var scriptPath = System.IO.Path.Combine(workspace, "eng", "package.ps1");
        var toolManifestPath = System.IO.Path.Combine(workspace, ".config", "dotnet-tools.json");
        var ignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.True(File.Exists(scriptPath), "The repository should own its packaging entry point.");
        Assert.True(File.Exists(toolManifestPath), "Packaging should use the pinned local vpk tool manifest.");
        var script = File.ReadAllText(scriptPath);
        Assert.Contains(script, "[ValidateSet(\"Stable\", \"Preview\")]", "The script should require an explicit edition.");
        Assert.Contains(script, "CastoPet.Preview", "Preview packages should use a distinct package identity.");
        Assert.Contains(script, "git status --porcelain", "Release packaging should reject uncommitted inputs by default.");
        Assert.Contains(script, "AllowDirty", "Local smoke tests should be able to opt into a clearly marked dirty build.");
        Assert.Contains(script, "dotnet", "Packaging should run through the pinned .NET SDK.");
        Assert.Contains(script, "publish", "Packaging should publish the application before invoking Velopack.");
        Assert.Contains(script, "tests/CastoPet.Tests/CastoPet.Tests.csproj", "Packaging should run the edition's Release tests before publishing.");
        Assert.Contains(script, "--self-contained", "Installer payloads should include the required .NET runtime.");
        Assert.Contains(script, "CastoPetEdition=$Edition", "Publishing should select the same Stable/Preview feature profile.");
        Assert.Contains(script, "tool", "Packaging should restore and invoke the local vpk tool.");
        Assert.Contains(script, "vpk", "Packaging should create a Velopack installer and update packages.");
        Assert.Contains(script, "build-metadata.json", "Every package should retain edition, source commit, and file hashes.");
        Assert.False(script.Contains("vpk upload", StringComparison.OrdinalIgnoreCase), "The local packaging script must not publish releases.");
        Assert.False(script.Contains("gh release", StringComparison.OrdinalIgnoreCase), "The local packaging script must not create GitHub releases.");
        Assert.Contains(ignore, "artifacts/packages/", "Generated installer payloads should remain outside source control.");
    }

    static void PackagingWorkflowProducesManualArtifactsWithoutPublishing()
    {
        var workspace = FindWorkspaceRoot();
        var workflowPath = System.IO.Path.Combine(workspace, ".github", "workflows", "package.yml");

        Assert.True(File.Exists(workflowPath), "Packaging should have a manually controlled CI workflow.");
        var workflow = File.ReadAllText(workflowPath);
        Assert.Contains(workflow, "workflow_dispatch:", "Installer generation should require an explicit manual dispatch.");
        Assert.Contains(workflow, "type: choice", "The workflow should make Stable/Preview selection explicit.");
        Assert.Contains(workflow, "eng/package.ps1", "CI and local packaging should share one implementation.");
        Assert.Contains(workflow, "actions/upload-artifact@v7", "The verified package should be exposed only as a short-lived workflow artifact.");
        Assert.Contains(workflow, "retention-days: 7", "Unsigned test packages should not be retained indefinitely.");
        Assert.False(workflow.Contains("gh release", StringComparison.OrdinalIgnoreCase), "The validation workflow must not publish a GitHub release.");
        Assert.False(workflow.Contains("vpk upload", StringComparison.OrdinalIgnoreCase), "The validation workflow must not upload to the release repository.");
    }

    static void RepositoryIgnoresLocalWorkingAssets()
    {
        var workspace = FindWorkspaceRoot();
        var gitignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.Contains(gitignore, "/.codex/", "Repository-local Codex state should remain untracked.");
        Assert.Contains(gitignore, "/artwork/references/", "Reference images should remain untracked outside the source tree.");
        Assert.Contains(gitignore, "artifacts/builds/", "Repository-local build artifacts should remain untracked.");
        Assert.Contains(gitignore, "artifacts/reports/", "Stability and archived task reports should remain untracked.");
        Assert.Contains(gitignore, "artifacts/temp/", "Temporary generated output should remain untracked.");
        Assert.Contains(gitignore, "artifacts/generation/*/runs/", "Large image-generation runs should remain untracked.");
    }

    static void RepositoryKeepsAuthoringArtworkOutsideSource()
    {
        var workspace = FindWorkspaceRoot();
        var sourceAssets = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets");
        var artwork = System.IO.Path.Combine(workspace, "artwork");
        var gitignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.False(Directory.Exists(System.IO.Path.Combine(sourceAssets, "CandidateSet")), "Candidate artwork should not live under the application source tree.");
        Assert.False(Directory.Exists(System.IO.Path.Combine(sourceAssets, "Skins")), "Editable skin artwork should not live under the application source tree.");
        Assert.True(Directory.Exists(System.IO.Path.Combine(artwork, "candidates", "Castorice")), "Reviewed candidate artwork should live under artwork/candidates/Castorice.");
        Assert.True(Directory.Exists(System.IO.Path.Combine(artwork, "authoring", "Castorice")), "Editable skin artwork should live under artwork/authoring/Castorice.");
        Assert.False(gitignore.Contains("/artwork/candidates/", StringComparison.Ordinal), "Reviewed candidate artwork must remain tracked by Git.");
    }

    static void ProductionCodeIsOrganizedByArchitecture()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var coreRoot = System.IO.Path.Combine(projectRoot, "Core");

        Assert.True(File.Exists(System.IO.Path.Combine(coreRoot, "Animation", "PetAnimationController.cs")), "Pure animation behavior should live under Core/Animation.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Application", "Updates", "UpdateCoordinator.cs")), "Update orchestration should live under Application/Updates.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "WindowsInputHookService.cs")), "Windows integrations should live under Infrastructure/Platform.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml")), "Pet window markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml")), "Settings window markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "CrashNotificationWindow.xaml")), "Crash notification markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Styling", "RadialWheelStyle.cs")), "Radial wheel styling should live under Presentation/Styling.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Styling", "SettingsThemePalette.cs")), "Settings colors should live under Presentation/Styling.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Shortcuts", "ShortcutIconService.cs")), "WPF shortcut icons should live under Presentation/Shortcuts.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "SettingsBackdropService.cs")), "Native Windows backdrop integration should live under Infrastructure/Platform.");
        Assert.False(File.Exists(System.IO.Path.Combine(projectRoot, "PetWindow.xaml")), "Window markup should not remain loose at the project root.");
        var legacyPresentationRoot = System.IO.Path.Combine(projectRoot, "Infrastructure", "Presentation");
        Assert.True(
            !Directory.Exists(legacyPresentationRoot) || !Directory.EnumerateFiles(legacyPresentationRoot, "*.cs", SearchOption.AllDirectories).Any(),
            "Infrastructure should not retain presentation-layer source files.");
        Assert.Equal(0, Directory.EnumerateFiles(coreRoot, "*.cs", SearchOption.TopDirectoryOnly).Count(), "Core should not retain ungrouped source files.");

        foreach (var layer in new[] { "Application", "Core", "Infrastructure", "Presentation" })
        {
            var layerRoot = System.IO.Path.Combine(projectRoot, layer);
            foreach (var sourcePath in Directory.EnumerateFiles(layerRoot, "*.cs", SearchOption.AllDirectories))
            {
                var relativeDirectory = System.IO.Path.GetRelativePath(projectRoot, System.IO.Path.GetDirectoryName(sourcePath)!);
                var expectedNamespace = $"namespace CastoPet.{relativeDirectory.Replace(System.IO.Path.DirectorySeparatorChar, '.')};";
                var source = File.ReadAllText(sourcePath);
                Assert.Contains(source, expectedNamespace, $"{System.IO.Path.GetRelativePath(projectRoot, sourcePath)} should match its architecture directory namespace.");
            }
        }
    }

    static void ArchitectureDependenciesPointInward()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");

        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Core"),
            "Core",
            "System.Windows",
            "CastoPet.Application",
            "CastoPet.Infrastructure",
            "CastoPet.Presentation");
        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Application"),
            "Application",
            "System.Windows",
            "CastoPet.Presentation");
        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Application", "Settings"),
            "Application/Settings",
            "CastoPet.Infrastructure");
        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Application", "Updates"),
            "Application/Updates",
            "CastoPet.Infrastructure");

        var settingsContract = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Application", "Settings", "ISettingsStore.cs"));
        var settingsImplementation = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Persistence", "SettingsService.cs"));
        var updateContract = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Application", "Updates", "IUpdateService.cs"));
        var updateImplementation = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Updates", "VelopackUpdateService.cs"));
        Assert.Contains(settingsContract, "interface ISettingsStore", "The settings persistence boundary should be owned by Application.");
        Assert.Contains(settingsImplementation, ": ISettingsStore", "Infrastructure should implement the settings persistence boundary.");
        Assert.Contains(updateContract, "interface IUpdateService", "The update boundary should be owned by Application.");
        Assert.Contains(updateImplementation, ": IUpdateService", "Infrastructure should implement the update boundary.");
    }

    static void AssertLayerDoesNotReference(string root, string layer, params string[] forbiddenReferences)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(sourcePath);
            foreach (var forbiddenReference in forbiddenReferences)
            {
                Assert.False(
                    source.Contains(forbiddenReference, StringComparison.Ordinal),
                    $"{layer} must not reference {forbiddenReference}: {System.IO.Path.GetRelativePath(root, sourcePath)}.");
            }
        }
    }

    static void VelopackRunsAtTheApplicationEntryPoint()
    {
        var workspace = FindWorkspaceRoot();
        var program = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Program.cs"));
        var app = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(program, "VelopackApp.Build().Run();", "Velopack hooks should run at the beginning of Main.");
        Assert.Contains(program, "static void Main", "The application should expose an explicit entry point.");
        Assert.False(app.Contains("VelopackApp.Build().Run();", StringComparison.Ordinal), "Velopack hooks should not wait until the App constructor.");
    }

    static void UpdateSourcePointsToThePublicReleasesRepository()
    {
        Assert.Equal(
            "https://github.com/sunboming/CastoPet-Releases",
            VelopackUpdateService.RepositoryUrl,
            "Installed builds should use the public releases repository without a client token.");
    }

}
