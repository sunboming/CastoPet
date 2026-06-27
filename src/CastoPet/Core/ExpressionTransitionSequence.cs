namespace CastoPet.Core;

public static class ExpressionTransitionSequence
{
    public const int InFrameCount = 2;
    public const int OutFrameCount = 2;
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(80);

    public static readonly IReadOnlyList<string> InFramePaths = Enumerable
        .Range(0, InFrameCount)
        .Select(index => $"Assets/Expressions/Transition/Castorice.ExpressionTransition.In.{index:00}.png")
        .ToArray();

    public static readonly IReadOnlyList<string> OutFramePaths = Enumerable
        .Range(0, OutFrameCount)
        .Select(index => $"Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.{index:00}.png")
        .ToArray();
}
