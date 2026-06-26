using CastoPet.Core;

var tests = new (string Name, Action Test)[]
{
    ("Default settings match MVP defaults", DefaultSettingsMatchMvpDefaults),
    ("Settings round trip as JSON", SettingsRoundTripAsJson),
    ("Invalid settings file falls back to defaults", InvalidSettingsFallsBackToDefaults),
    ("Logging writes a dated log file", LoggingWritesDatedLogFile),
    ("Bottom-right placement uses work area margin", BottomRightPlacementUsesWorkAreaMargin),
    ("Startup value name is CastoPet", StartupValueNameIsCastoPet),
    ("Single instance rejects a second owner", SingleInstanceRejectsSecondOwner),
    ("Single instance restore signal reaches primary", SingleInstanceRestoreSignalReachesPrimary),
    ("Runtime position starts at default", RuntimePositionStartsAtDefault),
    ("Runtime position tracks drag for current run only", RuntimePositionTracksDragForCurrentRunOnly),
    ("Show restore keeps hidden position but resets visible position", ShowRestoreKeepsHiddenPositionButResetsVisiblePosition),
    ("Idle frame sequence defines eight slow frame paths", IdleFrameSequenceDefinesEightSlowFramePaths),
    ("Blink frame sequence defines random blink frames", BlinkFrameSequenceDefinesRandomBlinkFrames),
    ("Character assets decode at pet display width", CharacterAssetsDecodeAtPetDisplayWidth),
    ("Packaged character assets are display sized", PackagedCharacterAssetsAreDisplaySized),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failures;

static void DefaultSettingsMatchMvpDefaults()
{
    var settings = AppSettings.Default;
    Assert.True(settings.Topmost, "Topmost should default to true.");
    Assert.False(settings.ClickThrough, "ClickThrough should default to false.");
    Assert.False(settings.ShowInTaskbar, "ShowInTaskbar should default to false.");
    Assert.False(settings.StartWithWindows, "StartWithWindows should default to false.");
}

static void SettingsRoundTripAsJson()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        StartWithWindows = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.False(loaded.Topmost, "Topmost should round trip.");
    Assert.True(loaded.ClickThrough, "ClickThrough should round trip.");
    Assert.True(loaded.ShowInTaskbar, "ShowInTaskbar should round trip.");
    Assert.True(loaded.StartWithWindows, "StartWithWindows should round trip.");
}

static void InvalidSettingsFallsBackToDefaults()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    Directory.CreateDirectory(paths.DataDirectory);
    File.WriteAllText(paths.SettingsFile, "{not valid json");

    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);
    var loaded = service.Load();

    Assert.True(loaded.Topmost, "Invalid settings should return defaults.");
    Assert.False(loaded.ClickThrough, "Invalid settings should return defaults.");
    Assert.True(File.Exists(paths.LogFile), "Invalid settings should be logged.");
}

static void LoggingWritesDatedLogFile()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);

    logger.Info("hello");

    Assert.True(File.Exists(paths.LogFile), "Log file should exist.");
    var text = File.ReadAllText(paths.LogFile);
    Assert.Contains(text, "hello", "Log file should include message.");
}

static void BottomRightPlacementUsesWorkAreaMargin()
{
    var bounds = WindowPlacementService.CalculateBottomRight(
        workAreaLeft: 0,
        workAreaTop: 0,
        workAreaWidth: 1920,
        workAreaHeight: 1080,
        windowWidth: 320,
        windowHeight: 420,
        margin: 24);

    Assert.Equal(1576, (int)bounds.Left, "Left should place window near the right edge.");
    Assert.Equal(636, (int)bounds.Top, "Top should place window near the bottom edge.");
}

static void StartupValueNameIsCastoPet()
{
    Assert.Equal("CastoPet", StartupService.ValueName, "Startup registry value should use app name.");
}

static void SingleInstanceRejectsSecondOwner()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var scope = "CastoPet.Tests." + Guid.NewGuid().ToString("N");

    using var first = new SingleInstanceService(logger, scope);
    using var second = new SingleInstanceService(logger, scope);

    Assert.True(first.IsPrimaryInstance, "First service should own the instance mutex.");
    Assert.False(second.IsPrimaryInstance, "Second service should not own the same instance mutex.");
}

static void SingleInstanceRestoreSignalReachesPrimary()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var scope = "CastoPet.Tests." + Guid.NewGuid().ToString("N");
    using var first = new SingleInstanceService(logger, scope);
    using var second = new SingleInstanceService(logger, scope);
    using var restored = new ManualResetEventSlim(false);

    first.StartRestoreServer(() => restored.Set());

    var signaled = second.SignalRestoreAsync().GetAwaiter().GetResult();

    Assert.True(signaled, "Second instance should signal primary without pipe errors.");
    Assert.True(restored.Wait(TimeSpan.FromSeconds(2)), "Primary should receive restore signal.");
}

static void RuntimePositionStartsAtDefault()
{
    var state = new PetRuntimeState();

    Assert.False(state.HasRuntimePosition, "New runtime state should not have a dragged position.");
}

static void RuntimePositionTracksDragForCurrentRunOnly()
{
    var state = new PetRuntimeState();

    state.SetRuntimePosition(120, 240);

    Assert.True(state.HasRuntimePosition, "Dragged position should be tracked during this run.");
    Assert.Equal(120d, state.Left, "Dragged left should be stored.");
    Assert.Equal(240d, state.Top, "Dragged top should be stored.");
}

static void ShowRestoreKeepsHiddenPositionButResetsVisiblePosition()
{
    var state = new PetRuntimeState();
    state.SetRuntimePosition(120, 240);

    var hiddenAction = state.GetShowRestoreAction(isVisible: false);
    var visibleAction = state.GetShowRestoreAction(isVisible: true);

    Assert.Equal(PetShowRestoreAction.ShowAtRuntimePosition, hiddenAction, "Hidden pet should reappear at current runtime position.");
    Assert.Equal(PetShowRestoreAction.RestoreDefaultPosition, visibleAction, "Visible pet should restore to default position.");
    Assert.False(state.HasRuntimePosition, "Restoring visible pet to default should clear runtime position.");
}

static void IdleFrameSequenceDefinesEightSlowFramePaths()
{
    Assert.Equal(8, IdleFrameSequence.FrameCount, "Idle should use eight frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(200), IdleFrameSequence.FrameInterval, "Idle frames should advance slowly.");
    Assert.Equal("Assets/States/Idle/Castorice.Idle.00.png", IdleFrameSequence.FramePaths[0], "First idle frame path should be zero padded.");
    Assert.Equal("Assets/States/Idle/Castorice.Idle.07.png", IdleFrameSequence.FramePaths[^1], "Last idle frame path should be zero padded.");
}

static void BlinkFrameSequenceDefinesRandomBlinkFrames()
{
    Assert.Equal(3, BlinkFrameSequence.FrameCount, "Blink should use three frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), BlinkFrameSequence.FrameInterval, "Blink frames should advance quickly.");
    Assert.Equal(TimeSpan.FromSeconds(3), BlinkFrameSequence.MinScheduleDelay, "Blink should not repeat too frequently.");
    Assert.Equal(TimeSpan.FromSeconds(7), BlinkFrameSequence.MaxScheduleDelay, "Blink should remain occasional.");
    Assert.Equal("Assets/States/Blink/Castorice.Blink.00.png", BlinkFrameSequence.FramePaths[0], "First blink frame path should be zero padded.");
    Assert.Equal("Assets/States/Blink/Castorice.Blink.02.png", BlinkFrameSequence.FramePaths[^1], "Last blink frame path should be zero padded.");
}

static void CharacterAssetsDecodeAtPetDisplayWidth()
{
    Assert.Equal(320, AssetService.CharacterDecodePixelWidth, "Character assets should decode near their display width to avoid full-size frame memory.");
}

static void PackagedCharacterAssetsAreDisplaySized()
{
    var workspace = FindWorkspaceRoot();
    var assets = Directory
        .EnumerateFiles(System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets"), "*.png", SearchOption.AllDirectories)
        .Where(path => !System.IO.Path.GetFileName(path).Equals("blink-preview.png", StringComparison.OrdinalIgnoreCase));

    foreach (var asset in assets)
    {
        var (width, height) = ReadPngSize(asset);

        Assert.True(
            width <= AssetService.CharacterDecodePixelWidth && height <= AssetService.CharacterDecodePixelWidth,
            $"{asset} should be no larger than {AssetService.CharacterDecodePixelWidth}px, got {width}x{height}.");
    }
}

static string FindWorkspaceRoot()
{
    var current = new DirectoryInfo(Environment.CurrentDirectory);
    while (current is not null)
    {
        if (File.Exists(System.IO.Path.Combine(current.FullName, "CastoPet.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find workspace root.");
}

static (int Width, int Height) ReadPngSize(string path)
{
    Span<byte> header = stackalloc byte[24];
    using var stream = File.OpenRead(path);
    if (stream.Read(header) != header.Length)
    {
        throw new InvalidOperationException($"{path} is not a valid PNG.");
    }

    var width = ReadBigEndianInt32(header[16..20]);
    var height = ReadBigEndianInt32(header[20..24]);
    return (width, height);
}

static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
{
    return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
}

static class Assert
{
    public static void True(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void False(bool value, string message)
    {
        if (value) throw new InvalidOperationException(message);
    }

    public static void Contains(string text, string expected, string message)
    {
        if (!text.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }
}

sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    private TempDirectory(string path)
    {
        Path = path;
    }

    public static TempDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CastoPet.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
