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
        var crashWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "CrashNotificationWindow.xaml"));
        var trayService = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "TrayService.cs"));
        var iconService = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "ApplicationIconService.cs"));

        Assert.Contains(project, @"<Resource Include=""Assets\AppIcon.ico"" />", "The shared icon should be available as a WPF resource.");
        Assert.Contains(petWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "The pet taskbar surface should use the shared icon.");
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

    static void ReleaseUsesMenusWithoutSettingsWindow()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var app = File.ReadAllText(System.IO.Path.Combine(projectRoot, "App.xaml.cs"));
        var petWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml.cs"));
        var tray = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "TrayService.cs"));

        Assert.False(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml")), "The basic release should not contain a separate settings window.");
        Assert.False(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml.cs")), "The basic release should not contain settings window code-behind.");
        Assert.False(File.Exists(System.IO.Path.Combine(projectRoot, "Application", "Settings", "SettingsWindowService.cs")), "The basic release should not retain settings window lifetime infrastructure.");
        Assert.False(app.Contains("SettingsWindow", StringComparison.Ordinal), "The application should not compose a settings window.");
        foreach (var command in new[] { "OpenCrashReportsText", "CheckForUpdatesText" })
        {
            Assert.Contains(petWindow, command, $"The pet context menu should expose {command}.");
            Assert.Contains(tray, command, $"The tray menu should expose {command}.");
        }
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

    static void ReleaseContainsOnlyBasicDesktopPetFeatures()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var project = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        var app = File.ReadAllText(System.IO.Path.Combine(projectRoot, "App.xaml.cs"));
        var settings = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Core", "Settings", "AppSettings.cs"));
        var catalog = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Application", "Settings", "SettingCatalog.cs"));
        var workflow = File.ReadAllText(System.IO.Path.Combine(workspace, ".github", "workflows", "build.yml"));

        Assert.False(project.Contains("CastoPetEdition", StringComparison.Ordinal), "The 0.1 branch should build one product without edition switches.");
        Assert.Contains(project, @"States\Idle\*.png", "The release should package idle frames.");
        Assert.Contains(project, @"States\Blink\*.png", "The release should package blink frames.");
        Assert.False(project.Contains(@"States\Castorice.Dragging.png", StringComparison.Ordinal), "The release should not package a dedicated dragging visual.");
        Assert.False(project.Contains(@"Assets\Runtime\Castorice\**\*.png", StringComparison.Ordinal), "The release must not package every experimental runtime asset.");
        var looseStateImages = Directory.GetFiles(
            System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States"),
            "*.png",
            SearchOption.TopDirectoryOnly);
        Assert.Equal(0, looseStateImages.Length, "The release runtime should not retain unpackaged expression or dragging images.");

        foreach (var excludedFeature in new[]
                 {
                     "CastoPetFeatureProfile",
                     "PetSkinSelectionService",
                     "ShortcutService",
                     "WheelCatalogService",
                     "ShortcutDropHandler",
                     "ShortcutLauncher",
                 })
        {
            Assert.False(app.Contains(excludedFeature, StringComparison.Ordinal), $"The composition root should not enable {excludedFeature}.");
        }

        foreach (var excludedSetting in new[] { "ActiveMovement", "PushCursor", "SkinManifestPath", "ThemeMode" })
        {
            Assert.False(settings.Contains(excludedSetting, StringComparison.Ordinal), $"The 0.1 settings model should not retain {excludedSetting}.");
            Assert.False(catalog.Contains(excludedSetting, StringComparison.Ordinal), $"The 0.1 settings catalog should not expose {excludedSetting}.");
        }

        Assert.False(workflow.Contains("edition:", StringComparison.OrdinalIgnoreCase), "CI should not build separate product editions.");
        Assert.False(workflow.Contains("CastoPetEdition", StringComparison.Ordinal), "CI should build the single 0.1 product directly.");
    }

    static void PackagingScriptBuildsTraceableInstaller()
    {
        var workspace = FindWorkspaceRoot();
        var scriptPath = System.IO.Path.Combine(workspace, "eng", "package.ps1");
        var toolManifestPath = System.IO.Path.Combine(workspace, ".config", "dotnet-tools.json");
        var ignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.True(File.Exists(scriptPath), "The repository should own its packaging entry point.");
        Assert.True(File.Exists(toolManifestPath), "Packaging should use the pinned local vpk tool manifest.");
        var script = File.ReadAllText(scriptPath);
        Assert.False(script.Contains("[ValidateSet(\"Stable\", \"Preview\")]", StringComparison.Ordinal), "The script should package the single product without an edition selector.");
        Assert.False(script.Contains("CastoPet.Preview", StringComparison.Ordinal), "The installer should use one public package identity.");
        Assert.Contains(script, "git status --porcelain", "Release packaging should reject uncommitted inputs by default.");
        Assert.Contains(script, "AllowDirty", "Local smoke tests should be able to opt into a clearly marked dirty build.");
        Assert.Contains(script, "dotnet", "Packaging should run through the pinned .NET SDK.");
        Assert.Contains(script, "publish", "Packaging should publish the application before invoking Velopack.");
        Assert.Contains(script, "tests/CastoPet.Tests/CastoPet.Tests.csproj", "Packaging should run Release tests before publishing.");
        Assert.Contains(script, "--self-contained", "Installer payloads should include the required .NET runtime.");
        Assert.False(script.Contains("CastoPetEdition", StringComparison.Ordinal), "Publishing should not select an obsolete edition profile.");
        Assert.Contains(script, "tool", "Packaging should restore and invoke the local vpk tool.");
        Assert.Contains(script, "vpk", "Packaging should create a Velopack installer and update packages.");
        Assert.Contains(script, "build-metadata.json", "Every package should retain version, source commit, and file hashes.");
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
        Assert.False(workflow.Contains("type: choice", StringComparison.Ordinal), "The workflow should package the single release product without an edition choice.");
        Assert.Contains(workflow, "eng/package.ps1", "CI and local packaging should share one implementation.");
        Assert.Contains(workflow, "actions/upload-artifact@v7", "The verified package should be exposed only as a short-lived workflow artifact.");
        Assert.Contains(workflow, "retention-days: 7", "Unsigned test packages should not be retained indefinitely.");
        Assert.False(workflow.Contains("gh release", StringComparison.OrdinalIgnoreCase), "The validation workflow must not publish a GitHub release.");
        Assert.False(workflow.Contains("vpk upload", StringComparison.OrdinalIgnoreCase), "The validation workflow must not upload to the release repository.");
    }

    static void ReleaseScriptCreatesDraftInSourceRepository()
    {
        var workspace = FindWorkspaceRoot();
        var scriptPath = System.IO.Path.Combine(workspace, "eng", "release.ps1");

        Assert.True(File.Exists(scriptPath), "The repository should provide one command for a controlled draft release.");
        var script = File.ReadAllText(scriptPath);
        Assert.Contains(script, "Directory.Build.props", "Official releases should match the committed repository version.");
        Assert.Contains(script, "git status --porcelain", "Official releases should reject dirty source trees.");
        Assert.Contains(script, "package.ps1", "Release creation should reuse the verified packaging entry point.");
        Assert.Contains(script, "sunboming/CastoPet", "Draft releases should be created in the public source repository.");
        Assert.Contains(script, "release", "The script should use GitHub Releases for distribution.");
        Assert.Contains(script, "create", "The script should create a release when one does not exist.");
        Assert.Contains(script, "--draft", "New releases must remain drafts until manually approved.");
        Assert.Contains(script, "--verify-tag", "Release creation should require a pushed Git tag.");
        Assert.Contains(script, "build-metadata.json", "The traceability metadata should be uploaded with Velopack assets.");
        Assert.False(script.Contains("--draft=false", StringComparison.OrdinalIgnoreCase), "The local helper must never publish a draft automatically.");
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

        Assert.True(File.Exists(System.IO.Path.Combine(coreRoot, "Animation", "PetFrameTiming.cs")), "Per-frame animation timing should live under Core/Animation.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Application", "Updates", "UpdateCoordinator.cs")), "Update orchestration should live under Application/Updates.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml")), "Pet window markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "CrashNotificationWindow.xaml")), "Crash notification markup should live under Presentation/Windows.");
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

    static void InputReactiveFeatureIsFullyRemoved()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var removedFiles = new[]
        {
            System.IO.Path.Combine(projectRoot, "Core", "Input", "InputKeyboardLayout.cs"),
            System.IO.Path.Combine(projectRoot, "Core", "Input", "InputReactiveEvent.cs"),
            System.IO.Path.Combine(projectRoot, "Core", "Input", "InputReactiveModePolicy.cs"),
            System.IO.Path.Combine(projectRoot, "Core", "Input", "InputReactiveState.cs"),
            System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "WindowsInputHookService.cs"),
        };

        foreach (var path in removedFiles)
        {
            Assert.False(File.Exists(path), $"Removed input-reactive file should not remain: {path}.");
        }

        Assert.False(
            Directory.Exists(System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "InputReactive")),
            "Input-reactive runtime artwork should not remain packaged.");

        var sourceFiles = Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .Where(path => new[] { ".cs", ".xaml", ".json", ".csproj" }
                .Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        foreach (var path in sourceFiles)
        {
            var source = File.ReadAllText(path);
            Assert.False(source.Contains("InputReactive", StringComparison.OrdinalIgnoreCase), $"Input-reactive production reference should be removed from {path}.");
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
            "https://github.com/sunboming/CastoPet",
            VelopackUpdateService.RepositoryUrl,
            "Installed builds should use the public source repository releases without a client token.");
    }

}
