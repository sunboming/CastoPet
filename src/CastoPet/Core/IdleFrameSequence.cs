namespace CastoPet.Core;

public static class IdleFrameSequence
{
    public const int FrameCount = 8;
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(200);

    public static readonly IReadOnlyList<string> FramePaths = Enumerable
        .Range(0, FrameCount)
        .Select(index => $"Assets/States/Idle/Castorice.Idle.{index:00}.png")
        .ToArray();
}
