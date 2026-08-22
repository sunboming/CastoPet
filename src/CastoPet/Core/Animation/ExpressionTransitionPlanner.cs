namespace CastoPet.Core.Animation;

public static class ExpressionTransitionPlanner
{
    public static IReadOnlyList<T> EnterFrames<T>(IReadOnlyList<T> specific, IReadOnlyList<T> fallback)
    {
        return specific.Count > 0 ? specific : fallback;
    }

    public static IReadOnlyList<T> ExitFrames<T>(IReadOnlyList<T> specific, IReadOnlyList<T> fallback)
    {
        return specific.Count > 0 ? specific.Reverse().ToArray() : fallback;
    }
}
