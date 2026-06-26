namespace CastoPet.Core;

public static class BlinkFrameSequence
{
    public const int FrameCount = 3;
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(90);
    public static readonly TimeSpan MinScheduleDelay = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan MaxScheduleDelay = TimeSpan.FromSeconds(7);

    public static readonly IReadOnlyList<string> FramePaths = Enumerable
        .Range(0, FrameCount)
        .Select(index => $"Assets/States/Blink/Castorice.Blink.{index:00}.png")
        .ToArray();
}
