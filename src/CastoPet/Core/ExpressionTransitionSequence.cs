namespace CastoPet.Core;

public static class ExpressionTransitionSequence
{
    public const int InFrameCount = 4;
    public const int OutFrameCount = 4;
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(55);

    public static readonly IReadOnlyList<string> InFramePaths = Enumerable
        .Range(0, InFrameCount)
        .Select(index => $"Assets/Expressions/Transition/Castorice.ExpressionTransition.In.{index:00}.png")
        .ToArray();

    public static readonly IReadOnlyList<string> OutFramePaths = Enumerable
        .Range(0, OutFrameCount)
        .Select(index => $"Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.{index:00}.png")
        .ToArray();
}
