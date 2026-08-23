namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void BuiltInIdleActionDefinesEightAuthoredRateFramePaths()
    {
        var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

        Assert.Equal(8, idle.FramePaths.Count, "Idle should use eight frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(125), idle.FrameInterval, "Idle frames should advance at the authored 8 FPS rate.");
        Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.00.png", idle.FramePaths[0], "First idle frame path should be zero padded.");
        Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.07.png", idle.FramePaths[^1], "Last idle frame path should be zero padded.");
    }

    static void IdleFrameDiagnosticsReadAllPackagedFrames()
    {
        var diagnostics = ReadIdleFrameDiagnostics();

        Assert.Equal(BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle).FramePaths.Count, diagnostics.Count, "Diagnostics should include all idle frames.");
        Assert.True(diagnostics.All(frame => frame.Width == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display width.");
        Assert.True(diagnostics.All(frame => frame.Height == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display height.");
        Assert.True(diagnostics.All(frame => frame.Bounds.Width > 0 && frame.Bounds.Height > 0), "Idle frames should have visible alpha bounds.");
        Assert.True(diagnostics.Max(frame => frame.Bounds.Bottom) - diagnostics.Min(frame => frame.Bounds.Bottom) <= 1, "Idle frame bottom edges should stay anchored.");
        Assert.True(diagnostics.Max(frame => frame.CenterX) - diagnostics.Min(frame => frame.CenterX) <= 1.0, "Idle frame centers should stay horizontally anchored.");
        Assert.Equal("Castorice.Idle.07.png", diagnostics[^1].Name, "Diagnostics should preserve frame order.");
    }

    static void BuiltInBlinkActionDefinesRandomBlinkFrames()
    {
        var blink = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Blink);

        Assert.Equal(5, blink.FramePaths.Count, "Blink should use five frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(45), blink.FrameInterval, "Blink frames should advance quickly.");
        Assert.Equal(TimeSpan.FromSeconds(3), blink.MinScheduleDelay, "Blink should not repeat too frequently.");
        Assert.Equal(TimeSpan.FromSeconds(7), blink.MaxScheduleDelay, "Blink should remain occasional.");
        Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.00.png", blink.FramePaths[0], "First blink frame path should be zero padded.");
        Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.04.png", blink.FramePaths[^1], "Last blink frame path should be zero padded.");
    }

    static void BuiltInMoveActionDefinesEightDistanceDrivenPaths()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        Assert.Equal(8, move.FramePaths.Count, "Move should use eight frames.");
        Assert.Equal(10d, move.DistancePerFrame, "Move frames should advance by travel distance.");
        Assert.Equal("Assets/Runtime/Castorice/States/Move/Castorice.Move.00.png", move.FramePaths[0], "First move frame path should be zero padded.");
        Assert.Equal("Assets/Runtime/Castorice/States/Move/Castorice.Move.07.png", move.FramePaths[^1], "Last move frame path should be zero padded.");
    }

    static void MoveFramePathsUseAppResources()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        for (var index = 0; index < move.FramePaths.Count; index++)
        {
            Assert.Equal($"Assets/Runtime/Castorice/States/Move/Castorice.Move.{index:00}.png", move.FramePaths[index], "Move frame should use the resource path convention.");
        }
    }

    static void MoveSpeedConstantsStayInSmoothRange()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        Assert.Equal(90d, move.BaseSpeedPixelsPerSecond, "Move speed should have a stable base.");
        Assert.Equal(80d, move.MinSpeedPixelsPerSecond, "Move speed lower bound should stay near the base.");
        Assert.Equal(105d, move.MaxSpeedPixelsPerSecond, "Move speed upper bound should stay near the base.");
    }
}
