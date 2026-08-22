namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void ShortcutServiceLoadsEmptyStateAndRoundTrips()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));

        service.Load();
        Assert.Equal(0, service.GetAll().Count, "Missing storage should load as empty.");
        var added = service.TryAdd(new ShortcutDefinition("editor", "Editor", ShortcutType.Program, @"C:\Tools\Editor.exe", "--project demo", @"C:\Tools", 0));
        Assert.True(added.Added, "A valid shortcut should be added.");
        Assert.True(File.Exists(paths.ShortcutsFile), "Shortcut data should be persisted.");

        var reloaded = new ShortcutService(paths, new LoggingService(paths));
        reloaded.Load();
        var item = reloaded.GetAll().Single();
        Assert.Equal(@"C:\Tools\Editor.exe", item.Target, "Target should round trip separately.");
        Assert.Equal("--project demo", item.Arguments, "Arguments should remain separate from target.");
    }

    static void ShortcutServiceNormalizesDuplicateIdentities()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();

        Assert.True(service.TryAdd(new ShortcutDefinition("file-a", "File", ShortcutType.File, @"C:\Work\..\Work\Readme.txt", "", null, 0)).Added, "First path should be added.");
        Assert.False(service.TryAdd(new ShortcutDefinition("file-b", "File duplicate", ShortcutType.File, @"c:\work\README.TXT", "", null, 0)).Added, "Equivalent Windows paths should be duplicates.");
        Assert.True(service.TryAdd(new ShortcutDefinition("web-a", "Web", ShortcutType.WebUrl, "HTTPS://Example.com:443/docs/", "", null, 0)).Added, "First URL should be added.");
        Assert.False(service.TryAdd(new ShortcutDefinition("web-b", "Web duplicate", ShortcutType.WebUrl, "https://example.com/docs", "", null, 0)).Added, "Equivalent URLs should be duplicates.");
        Assert.True(service.TryAdd(new ShortcutDefinition("link-a", "Link", ShortcutType.WindowsShortcut, @"C:\Links\Tool.lnk", "", null, 0)).Added, "First link should be added.");
        Assert.False(service.TryAdd(new ShortcutDefinition("link-b", "Link duplicate", ShortcutType.WindowsShortcut, @"c:\links\TOOL.LNK", "", null, 0)).Added, "Link identity should use its own path.");
    }

    static void ShortcutServiceAppendsCandidatesWithContiguousOrdering()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();
        service.TryAdd(new ShortcutDefinition("first", "First", ShortcutType.File, @"C:\First.txt", "", null, 40));
        service.TryAdd(new ShortcutDefinition("second", "Second", ShortcutType.File, @"C:\Second.txt", "", null, -100));

        var entries = service.GetAll();
        Assert.Equal("first", entries[0].Id, "A candidate-provided sort order must not insert before existing entries.");
        Assert.Equal("second", entries[1].Id, "New shortcuts should append after existing entries.");
        Assert.True(new[] { 0, 1 }.SequenceEqual(entries.Select(entry => entry.SortOrder)), "Persisted sort orders should remain contiguous.");
    }

    static void ShortcutServiceMutatesOrderedEntries()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();
        service.TryAdd(new ShortcutDefinition("a", "A", ShortcutType.File, @"C:\A.txt", "", null, 5));
        service.TryAdd(new ShortcutDefinition("b", "B", ShortcutType.File, @"C:\B.txt", "", null, 2));

        Assert.Equal("a", service.GetAll()[0].Id, "New entries should append regardless of candidate sort order.");
        Assert.True(service.Rename("a", "Renamed").Succeeded, "Rename should succeed.");
        Assert.True(service.Move("b", 0).Succeeded, "Reorder should succeed.");
        Assert.Equal("b", service.GetAll()[0].Id, "Moved entry should occupy requested index.");
        Assert.Equal("Renamed", service.GetAll()[1].Name, "Rename should persist through reorder.");
        Assert.True(service.Delete("b").Succeeded, "Delete should succeed.");
        Assert.Equal(1, service.GetAll().Count, "Deleted entry should be removed.");
        Assert.Equal("Renamed", service.GetAll()[0].Name, "Remaining entry should preserve its edited name.");
    }

    static void ShortcutServiceUpdatesProgramLaunchOptionsSafely()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();
        var workingDirectory = System.IO.Path.Combine(temp.Path, "Workspace");
        Directory.CreateDirectory(workingDirectory);
        service.TryAdd(new ShortcutDefinition("program", "Program", ShortcutType.Program, @"C:\Tools\Editor.exe", "", null, 0));
        service.TryAdd(new ShortcutDefinition("file", "File", ShortcutType.File, @"C:\Notes.txt", "", null, 1));
        var changed = 0;
        service.Changed += (_, _) => changed++;

        var updated = service.UpdateLaunchOptions("program", "--project demo", $"  {workingDirectory}  ");

        Assert.True(updated.Succeeded, "Program launch options should be persisted.");
        var program = service.GetAll().Single(entry => entry.Id == "program");
        Assert.Equal("--project demo", program.Arguments, "Arguments should remain separate from the target.");
        Assert.Equal(workingDirectory, program.WorkingDirectory!, "Working directory should be trimmed before persistence.");
        Assert.Equal(1, changed, "A successful launch-option update should notify once.");

        var rejectedType = service.UpdateLaunchOptions("file", "--unsafe", workingDirectory);
        var rejectedDirectory = service.UpdateLaunchOptions("program", "changed", System.IO.Path.Combine(temp.Path, "Missing"));

        Assert.False(rejectedType.Succeeded, "Non-program entries must reject launch options.");
        Assert.False(rejectedDirectory.Succeeded, "A missing working directory must be rejected.");
        Assert.Equal(1, changed, "Rejected updates should not notify or mutate state.");
        program = service.GetAll().Single(entry => entry.Id == "program");
        Assert.Equal("--project demo", program.Arguments, "Rejected updates must preserve existing arguments.");
        Assert.Equal(2, service.GetAll().Count, "Validation must not auto-delete any shortcut.");
    }

    static void ShortcutServiceEnforcesEntryLimit()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();
        for (var index = 0; index < ShortcutService.MaxEntries; index++)
        {
            Assert.True(service.TryAdd(new ShortcutDefinition($"id-{index}", $"Item {index}", ShortcutType.File, $@"C:\Items\{index}.txt", "", null, index)).Added, "Entries within the limit should be added.");
        }

        Assert.False(service.TryAdd(new ShortcutDefinition("overflow", "Overflow", ShortcutType.File, @"C:\overflow.txt", "", null, 129)).Added, "The 129th entry should be rejected.");
        Assert.Equal(ShortcutService.MaxEntries, service.GetAll().Count, "Rejected entries must not alter state.");
    }

    static void ShortcutServiceRecoversMalformedStorage()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        Directory.CreateDirectory(paths.ShortcutsDirectory);
        File.WriteAllText(paths.ShortcutsFile, "{ malformed");
        var service = new ShortcutService(paths, new LoggingService(paths));

        service.Load();

        Assert.Equal(0, service.GetAll().Count, "Malformed storage should recover to empty.");
        Assert.Equal(1, Directory.GetFiles(paths.ShortcutsDirectory, "shortcuts.invalid-*.json").Length, "Malformed storage should receive a timestamped backup.");
    }

    static void ShortcutServiceIsolatesMalformedEntries()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        Directory.CreateDirectory(paths.ShortcutsDirectory);
        File.WriteAllText(paths.ShortcutsFile, """
            [
              { "id":"good", "name":"Good", "type":"File", "target":"C:\\\\Good.txt", "arguments":"", "workingDirectory":null, "sortOrder":0 },
              { "id":"bad", "name":42, "type":"Unknown", "target":null }
            ]
            """);
        var service = new ShortcutService(paths, new LoggingService(paths));

        service.Load();

        Assert.Equal(1, service.GetAll().Count, "A malformed entry should not discard valid siblings.");
        Assert.Equal("good", service.GetAll()[0].Id, "The valid entry should survive.");
    }

    static void ShortcutServiceNotifiesOnlyAfterPersistedMutations()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();
        var changed = 0;
        service.Changed += (_, _) => changed++;

        Assert.True(service.TryAdd(new ShortcutDefinition("a", "A", ShortcutType.File, @"C:\A.txt", "", null, 0)).Added, "Successful mutation should be reported.");
        Assert.Equal(1, changed, "Successful persisted mutation should notify once.");
        Assert.False(service.TryAdd(new ShortcutDefinition("duplicate", "A", ShortcutType.File, @"c:\a.TXT", "", null, 0)).Added, "Duplicate should fail without mutation.");
        Assert.Equal(1, changed, "Rejected mutation should not notify.");

        using var blocked = TempDirectory.Create();
        var blockedRoot = System.IO.Path.Combine(blocked.Path, "not-a-directory");
        File.WriteAllText(blockedRoot, "blocked");
        var blockedPaths = new AppPaths(blockedRoot);
        var blockedService = new ShortcutService(blockedPaths, new LoggingService(blockedPaths));
        var blockedChanges = 0;
        blockedService.Changed += (_, _) => blockedChanges++;
        var failed = blockedService.TryAdd(new ShortcutDefinition("failed", "Failed", ShortcutType.File, @"C:\Failed.txt", "", null, 0));
        Assert.False(failed.Succeeded, "File-system failures should be contained.");
        Assert.Equal(0, blockedChanges, "Failed persistence must not notify.");
        Assert.Equal(0, blockedService.GetAll().Count, "Failed persistence must not alter memory.");
    }

    static void ShortcutDropHandlerClassifiesExistingFileSystemItems()
    {
        using var temp = TempDirectory.Create();
        var executablePath = System.IO.Path.Combine(temp.Path, "Editor.ExE");
        var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
        var folderPath = System.IO.Path.Combine(temp.Path, "Archive.exe");
        var linkPath = System.IO.Path.Combine(temp.Path, "Launch.lNk");
        File.WriteAllText(executablePath, "executable fixture");
        File.WriteAllText(filePath, "ordinary file fixture");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(linkPath, "shortcut fixture");
        var service = CreateShortcutService(temp.Path);
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems([executablePath, filePath, folderPath, linkPath], []);

        Assert.Equal(4, result.AddedCount, "Every supported file-system item should be added.");
        Assert.Equal(0, result.DuplicateCount, "Distinct file-system items should not be duplicates.");
        Assert.Equal(0, result.UnsupportedCount, "Supported file-system items should not be rejected.");
        Assert.Equal(0, result.FailedCount, "Supported file-system items should not fail.");
        var entries = service.GetAll().ToDictionary(entry => entry.Target, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ShortcutType.Program, entries[executablePath].Type, "Executable extensions should be matched case-insensitively.");
        Assert.Equal("Editor", entries[executablePath].Name, "Executable names should omit their extension.");
        Assert.Equal(ShortcutType.File, entries[filePath].Type, "Ordinary files should remain files.");
        Assert.Equal("notes.txt", entries[filePath].Name, "Ordinary file names should retain their extension.");
        Assert.Equal(ShortcutType.Folder, entries[folderPath].Type, "Directories must be classified before their extension.");
        Assert.Equal(ShortcutType.WindowsShortcut, entries[linkPath].Type, "Windows shortcuts should retain their own type.");
        Assert.Equal(linkPath, entries[linkPath].Target, "Windows shortcuts should retain their own path.");
        Assert.Equal("executable fixture", File.ReadAllText(executablePath), "Drop recognition must not modify executable files.");
        Assert.Equal("ordinary file fixture", File.ReadAllText(filePath), "Drop recognition must not modify ordinary files.");
        Assert.True(Directory.Exists(folderPath), "Drop recognition must not move or remove directories.");
        Assert.Equal("shortcut fixture", File.ReadAllText(linkPath), "Drop recognition must not modify Windows shortcuts.");
    }

    static void ShortcutDropHandlerRejectsExecutableScripts()
    {
        using var temp = TempDirectory.Create();
        var deniedExtensions = new[]
        {
            ".bat", ".CMD", ".Ps1", ".vbs", ".JS", ".jse",
            ".wsf", ".WSH", ".hta", ".COM", ".scr", ".MSI",
        };
        var deniedPaths = deniedExtensions
            .Select((extension, index) => System.IO.Path.Combine(temp.Path, $"Denied-{index}{extension}"))
            .ToArray();
        foreach (var path in deniedPaths)
        {
            File.WriteAllText(path, "denied fixture");
        }

        var textPath = System.IO.Path.Combine(temp.Path, "Readme.txt");
        var executablePath = System.IO.Path.Combine(temp.Path, "Editor.EXE");
        var linkPath = System.IO.Path.Combine(temp.Path, "Editor.LnK");
        File.WriteAllText(textPath, "text fixture");
        File.WriteAllText(executablePath, "executable fixture");
        File.WriteAllText(linkPath, "shortcut fixture");
        var service = CreateShortcutService(temp.Path);
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems([.. deniedPaths, textPath, executablePath, linkPath], []);

        Assert.Equal(3, result.AddedCount, "Only the ordinary file, executable, and Windows shortcut should be added.");
        Assert.Equal(0, result.DuplicateCount, "Distinct safety fixtures should not be duplicates.");
        Assert.Equal(deniedPaths.Length, result.UnsupportedCount, "Every executable script or installer extension should be unsupported.");
        Assert.Equal(0, result.FailedCount, "Safety rejections should not be reported as storage failures.");
        var entries = service.GetAll();
        Assert.Equal(3, entries.Count, "Denied executable content must not be persisted.");
        Assert.Equal(ShortcutType.File, entries.Single(entry => entry.Target == textPath).Type, "Ordinary text files should remain allowed.");
        Assert.Equal(ShortcutType.Program, entries.Single(entry => entry.Target == executablePath).Type, "Explicit executable programs should remain allowed.");
        Assert.Equal(ShortcutType.WindowsShortcut, entries.Single(entry => entry.Target == linkPath).Type, "Windows shortcuts should remain explicitly allowed.");
        Assert.False(entries.Any(entry => deniedPaths.Contains(entry.Target, StringComparer.OrdinalIgnoreCase)), "Denied paths must never enter shortcut storage.");
        Assert.True(deniedPaths.All(File.Exists), "Rejected files must remain untouched.");
    }

    static void ShortcutDropHandlerAcceptsSafeWebTargets()
    {
        using var temp = TempDirectory.Create();
        var internetShortcutPath = System.IO.Path.Combine(temp.Path, "Docs.URL");
        var internetShortcutContents = "[InternetShortcut]\r\n URL = https://example.com/docs \r\nIconIndex=0\r\n";
        File.WriteAllText(internetShortcutPath, internetShortcutContents);
        var service = CreateShortcutService(temp.Path);
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems(
            [internetShortcutPath],
            [" HTTP://example.org/start ", "https://sub.example.net/path?q=1"]);

        Assert.Equal(3, result.AddedCount, "Internet shortcuts and direct HTTP/HTTPS text should be added.");
        Assert.Equal(0, result.DuplicateCount, "Distinct web targets should not be duplicates.");
        Assert.Equal(0, result.UnsupportedCount, "HTTP and HTTPS targets should be supported.");
        Assert.Equal(0, result.FailedCount, "Valid web targets should not fail.");
        var entries = service.GetAll();
        Assert.True(entries.All(entry => entry.Type == ShortcutType.WebUrl), "Every accepted web target should use the web URL type.");
        Assert.Equal("Docs", entries.Single(entry => entry.Target == "https://example.com/docs").Name, "Internet shortcuts should use their file name.");
        Assert.Equal("example.org", entries.Single(entry => entry.Target == "HTTP://example.org/start").Name, "Direct web targets should use a readable host name.");
        Assert.True(entries.All(entry => !string.IsNullOrWhiteSpace(entry.Id)), "Generated shortcut IDs should be non-empty.");
        Assert.Equal(internetShortcutContents, File.ReadAllText(internetShortcutPath), "Reading an internet shortcut must not modify it.");
    }

    static void ShortcutDropHandlerAcceptsSteamGameUris()
    {
        using var temp = TempDirectory.Create();
        var service = CreateShortcutService(temp.Path);
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems([], [" steam://rungameid/3419430 "]);

        Assert.Equal(1, result.AddedCount, "A Steam rungameid URI should be added.");
        Assert.Equal(0, result.UnsupportedCount, "A valid Steam game URI should not be rejected.");
        var entry = service.GetAll().Single();
        Assert.Equal(ShortcutType.SteamGame, entry.Type, "Steam game URIs should retain a constrained type.");
        Assert.Equal("steam://rungameid/3419430", entry.Target, "The original Steam game URI should be persisted.");
        Assert.Equal("Steam 3419430", entry.Name, "Steam game URIs should have a readable fallback name.");

        var duplicates = handler.AddDroppedItems([], ["STEAM://RUNGAMEID/3419430"]);
        Assert.Equal(1, duplicates.DuplicateCount, "Equivalent Steam game URIs should be duplicates.");

        var iconPath = System.IO.Path.Combine(temp.Path, "game.ico");
        File.WriteAllBytes(iconPath, [0]);
        var internetShortcutPath = System.IO.Path.Combine(temp.Path, "Bongo Cat.url");
        File.WriteAllText(
            internetShortcutPath,
            $"[InternetShortcut]\r\nURL=steam://rungameid/3419430\r\nIconFile={iconPath}\r\nIconIndex=0\r\n");

        var shortcutResult = handler.AddDroppedItems([internetShortcutPath], []);
        Assert.Equal(1, shortcutResult.DuplicateCount, "A Steam .url shortcut should match an existing URI entry.");
        var enrichedEntry = service.GetAll().Single();
        Assert.Equal("Bongo Cat", enrichedEntry.Name, "A Steam .url shortcut should enrich the fallback name.");
        Assert.Equal(iconPath, enrichedEntry.IconPath, "A Steam .url shortcut should enrich the game icon path.");
    }

    static void ShortcutDropHandlerRejectsMissingAndUnsafeInputs()
    {
        using var temp = TempDirectory.Create();
        var unsafeInternetShortcut = System.IO.Path.Combine(temp.Path, "Unsafe.url");
        var malformedInternetShortcut = System.IO.Path.Combine(temp.Path, "Malformed.url");
        File.WriteAllText(unsafeInternetShortcut, "[InternetShortcut]\nURL=ftp://example.com/file\n");
        File.WriteAllText(malformedInternetShortcut, "[InternetShortcut]\nIconIndex=0\n");
        var missingPath = System.IO.Path.Combine(temp.Path, "missing.txt");
        var service = CreateShortcutService(temp.Path);
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems(
            [missingPath, unsafeInternetShortcut, malformedInternetShortcut, ""],
            ["ftp://example.com/file", "file:///C:/secret.txt", "javascript:alert(1)", "cmd.exe /c calc", "not a URL"]);

        Assert.Equal(0, result.AddedCount, "Unsafe and missing inputs must not be added.");
        Assert.Equal(0, result.DuplicateCount, "Rejected inputs are not duplicates.");
        Assert.Equal(9, result.UnsupportedCount, "Every missing, malformed, unsafe, or arbitrary input should be reported unsupported.");
        Assert.Equal(0, result.FailedCount, "Parser rejections should not be reported as storage failures.");
        Assert.Equal(0, service.GetAll().Count, "Rejected text must never become a command shortcut.");
        Assert.True(File.Exists(unsafeInternetShortcut), "Rejected internet shortcuts must remain untouched.");
        Assert.True(File.Exists(malformedInternetShortcut), "Malformed internet shortcuts must remain untouched.");
    }

    static void ShortcutDropHandlerAggregatesMixedBatchDuplicates()
    {
        using var temp = TempDirectory.Create();
        var filePath = System.IO.Path.Combine(temp.Path, "Report.txt");
        var internetShortcutPath = System.IO.Path.Combine(temp.Path, "Portal.url");
        File.WriteAllText(filePath, "report");
        File.WriteAllText(internetShortcutPath, "[InternetShortcut]\nURL=https://example.com/portal/\n");
        var service = CreateShortcutService(temp.Path);
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems(
            [filePath, filePath, internetShortcutPath],
            ["https://EXAMPLE.com:443/portal", "ftp://example.com/portal"]);

        Assert.Equal(2, result.AddedCount, "The mixed batch should add one file and one web target.");
        Assert.Equal(2, result.DuplicateCount, "Repeated paths and normalized URLs should be counted as duplicates.");
        Assert.Equal(1, result.UnsupportedCount, "The unsupported scheme should be counted once.");
        Assert.Equal(0, result.FailedCount, "The mixed batch should not contain storage failures.");
        Assert.Equal(5, result.AddedCount + result.DuplicateCount + result.UnsupportedCount + result.FailedCount, "Aggregate counts should concisely account for every dropped value.");
        Assert.Equal(2, service.GetAll().Count, "Duplicates and unsupported values must not create entries.");
    }

    static void ShortcutDropHandlerReportsShortcutLimitFailures()
    {
        using var temp = TempDirectory.Create();
        var service = CreateShortcutService(temp.Path);
        for (var index = 0; index < ShortcutService.MaxEntries; index++)
        {
            var seed = new ShortcutDefinition($"seed-{index}", $"Seed {index}", ShortcutType.File, $@"C:\Seed\{index}.txt", "", null, index);
            Assert.True(service.TryAdd(seed).Added, "Limit test setup should fill the shortcut service.");
        }

        var overflowPath = System.IO.Path.Combine(temp.Path, "Overflow.exe");
        File.WriteAllText(overflowPath, "overflow fixture");
        var handler = new ShortcutDropHandler(service);

        var result = handler.AddDroppedItems([overflowPath], []);

        Assert.Equal(0, result.AddedCount, "Items beyond the shortcut limit must not be added.");
        Assert.Equal(0, result.DuplicateCount, "A new target beyond the limit is not a duplicate.");
        Assert.Equal(0, result.UnsupportedCount, "A valid executable remains supported at the limit.");
        Assert.Equal(1, result.FailedCount, "The shortcut limit should be reported as a failed addition.");
        Assert.Equal(ShortcutService.MaxEntries, service.GetAll().Count, "A limit failure must not alter stored entries.");
        Assert.Equal("overflow fixture", File.ReadAllText(overflowPath), "A limit failure must not modify the dropped executable.");
    }

    static ShortcutService CreateShortcutService(string baseDirectory)
    {
        var paths = new AppPaths(baseDirectory);
        var service = new ShortcutService(paths, new LoggingService(paths));
        service.Load();
        return service;
    }

    static void ShortcutLauncherCreatesStructuredShellStartInfo()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var programPath = System.IO.Path.Combine(temp.Path, "Editor.EXE");
        var workingDirectory = System.IO.Path.Combine(temp.Path, "Workspace");
        File.WriteAllText(programPath, "program fixture");
        Directory.CreateDirectory(workingDirectory);
        var launcher = new ShortcutLauncher(new LoggingService(paths), _ => null);
        var program = new ShortcutDefinition(
            "editor",
            "Editor",
            ShortcutType.Program,
            programPath,
            "--project \"demo file\"",
            workingDirectory,
            0);

        var info = launcher.CreateStartInfo(program);

        Assert.Equal(program.Target, info.FileName, "Target must remain a structured filename.");
        Assert.Equal(program.Arguments, info.Arguments, "Arguments must remain separate from the target.");
        Assert.Equal(workingDirectory, info.WorkingDirectory, "An explicit working directory should be preserved.");
        Assert.True(info.UseShellExecute, "Windows shell behavior should open associated targets.");
        Assert.False(info.Verb.Equals("runas", StringComparison.OrdinalIgnoreCase), "Shortcut launching must never request elevation.");

        var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
        File.WriteAllText(filePath, "file fixture");
        var withoutWorkingDirectory = launcher.CreateStartInfo(
            new ShortcutDefinition("notes", "Notes", ShortcutType.File, filePath, "", null, 1));
        Assert.True(string.IsNullOrEmpty(withoutWorkingDirectory.WorkingDirectory), "Working directory should remain optional.");
    }

    static void ShortcutLauncherAcceptsEverySupportedTargetType()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var programPath = System.IO.Path.Combine(temp.Path, "Editor.exe");
        var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
        var folderPath = System.IO.Path.Combine(temp.Path, "Documents");
        var linkPath = System.IO.Path.Combine(temp.Path, "Editor.lnk");
        File.WriteAllText(programPath, "program fixture");
        File.WriteAllText(filePath, "file fixture");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(linkPath, "shortcut fixture");
        var startCount = 0;
        var launcher = new ShortcutLauncher(
            new LoggingService(paths),
            _ =>
            {
                startCount++;
                return null;
            });
        var definitions = new[]
        {
            new ShortcutDefinition("program", "Program", ShortcutType.Program, programPath, "", null, 0),
            new ShortcutDefinition("file", "File", ShortcutType.File, filePath, "", null, 1),
            new ShortcutDefinition("folder", "Folder", ShortcutType.Folder, folderPath, "", null, 2),
            new ShortcutDefinition("link", "Link", ShortcutType.WindowsShortcut, linkPath, "", null, 3),
            new ShortcutDefinition("http", "HTTP", ShortcutType.WebUrl, "http://example.com/start", "", null, 4),
            new ShortcutDefinition("https", "HTTPS", ShortcutType.WebUrl, "https://example.com/docs", "", null, 5),
            new ShortcutDefinition("steam", "Steam", ShortcutType.SteamGame, "steam://rungameid/3419430", "", null, 6),
        };

        foreach (var definition in definitions)
        {
            var result = launcher.Launch(definition);
            Assert.True(result.Succeeded, $"{definition.Type} should be launchable when its target is valid.");
        }

        Assert.Equal(definitions.Length, startCount, "Every valid definition should reach the injected process boundary once.");
    }

    static void ShortcutLauncherRejectsMissingAndMalformedDefinitions()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var existingFile = System.IO.Path.Combine(temp.Path, "not-a-program.txt");
        var existingExecutable = System.IO.Path.Combine(temp.Path, "program.exe");
        var existingFolder = System.IO.Path.Combine(temp.Path, "Folder");
        File.WriteAllText(existingFile, "file fixture");
        File.WriteAllText(existingExecutable, "program fixture");
        Directory.CreateDirectory(existingFolder);
        var startCount = 0;
        var launcher = new ShortcutLauncher(
            new LoggingService(paths),
            _ =>
            {
                startCount++;
                return null;
            });
        var invalidDefinitions = new[]
        {
            new ShortcutDefinition("missing-program", "Missing", ShortcutType.Program, System.IO.Path.Combine(temp.Path, "missing.exe"), "", null, 0),
            new ShortcutDefinition("wrong-program", "Wrong", ShortcutType.Program, existingFile, "", null, 1),
            new ShortcutDefinition("missing-workdir", "Workdir", ShortcutType.Program, existingExecutable, "", System.IO.Path.Combine(temp.Path, "MissingWorkdir"), 2),
            new ShortcutDefinition("missing-file", "Missing", ShortcutType.File, System.IO.Path.Combine(temp.Path, "missing.txt"), "", null, 2),
            new ShortcutDefinition("folder-as-file", "Wrong", ShortcutType.File, existingFolder, "", null, 3),
            new ShortcutDefinition("missing-folder", "Missing", ShortcutType.Folder, System.IO.Path.Combine(temp.Path, "MissingFolder"), "", null, 4),
            new ShortcutDefinition("file-as-folder", "Wrong", ShortcutType.Folder, existingFile, "", null, 5),
            new ShortcutDefinition("missing-link", "Missing", ShortcutType.WindowsShortcut, System.IO.Path.Combine(temp.Path, "missing.lnk"), "", null, 6),
            new ShortcutDefinition("wrong-link", "Wrong", ShortcutType.WindowsShortcut, existingFile, "", null, 7),
            new ShortcutDefinition("exe-as-file", "Wrong", ShortcutType.File, existingExecutable, "", null, 8),
            new ShortcutDefinition("ftp", "FTP", ShortcutType.WebUrl, "ftp://example.com/file", "", null, 9),
            new ShortcutDefinition("relative", "Relative", ShortcutType.WebUrl, "example.com/path", "", null, 10),
            new ShortcutDefinition("hostless", "Hostless", ShortcutType.WebUrl, "http:///path", "", null, 11),
            new ShortcutDefinition("steam-command", "Steam", ShortcutType.SteamGame, "steam://open/console", "", null, 12),
            new ShortcutDefinition("steam-injection", "Steam", ShortcutType.SteamGame, "steam://rungameid/3419430?x=1", "", null, 13),
            new ShortcutDefinition("unknown", "Unknown", (ShortcutType)999, existingFile, "", null, 12),
        };

        foreach (var definition in invalidDefinitions)
        {
            var result = launcher.Launch(definition);
            Assert.False(result.Succeeded, $"Invalid {definition.Id} definition should return a failure.");
            Assert.True(!string.IsNullOrWhiteSpace(result.Error), $"Invalid {definition.Id} definition should explain its failure.");
        }

        Assert.Equal(0, startCount, "Rejected definitions must never reach the process boundary.");
    }

    static void ShortcutLauncherRejectsTamperedExecutableFileDefinitions()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var deniedExtensions = new[]
        {
            ".bat", ".CMD", ".Ps1", ".vbs", ".JS", ".jse",
            ".wsf", ".WSH", ".hta", ".COM", ".scr", ".MSI",
        };
        var startCount = 0;
        var launcher = new ShortcutLauncher(
            new LoggingService(paths),
            _ =>
            {
                startCount++;
                return null;
            });

        foreach (var extension in deniedExtensions)
        {
            var path = System.IO.Path.Combine(temp.Path, $"Tampered{extension}");
            File.WriteAllText(path, "denied fixture");
            var definition = new ShortcutDefinition("tampered", "Tampered", ShortcutType.File, path, "", null, 0);

            var result = launcher.Launch(definition);

            Assert.False(result.Succeeded, $"A File definition must reject executable extension {extension}.");
            Assert.True(!string.IsNullOrWhiteSpace(result.Error), "A safety rejection should include an error message.");
        }

        var linkPath = System.IO.Path.Combine(temp.Path, "Allowed.lnk");
        File.WriteAllText(linkPath, "shortcut fixture");
        var linkResult = launcher.Launch(
            new ShortcutDefinition("link", "Link", ShortcutType.WindowsShortcut, linkPath, "", null, 0));
        Assert.True(linkResult.Succeeded, "An explicit Windows shortcut definition should remain allowed.");
        Assert.Equal(1, startCount, "Only the explicit Windows shortcut should reach the process boundary.");
    }

    static void ShortcutLauncherContainsAndLogsStartFailures()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var filePath = System.IO.Path.Combine(temp.Path, "notes.txt");
        File.WriteAllText(filePath, "file fixture");
        var launcher = new ShortcutLauncher(
            new LoggingService(paths),
            _ => throw new InvalidOperationException("simulated start failure"));

        var result = launcher.Launch(
            new ShortcutDefinition("notes", "Notes", ShortcutType.File, filePath, "", null, 0));

        Assert.False(result.Succeeded, "Process start exceptions should be contained as failures.");
        Assert.True(!string.IsNullOrWhiteSpace(result.Error), "Contained process failures should include an error message.");
        Assert.True(File.Exists(paths.LogFile), "Contained process failures should be logged.");
        var log = File.ReadAllText(paths.LogFile);
        Assert.Contains(log, "simulated start failure", "The log should retain the process exception details.");
        Assert.Contains(log, filePath, "The log should identify the target that failed to launch.");
    }
}
