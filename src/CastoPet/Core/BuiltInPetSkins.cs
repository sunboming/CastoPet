namespace CastoPet.Core;

public static class BuiltInPetSkins
{
    private const string CastoriceRuntimeRoot = "Assets/Runtime/Castorice";

    public static readonly PetSkinDefinition Castorice = new(
        Id: "castorice",
        DisplayName: "Castorice",
        ResourceRoot: CastoriceRuntimeRoot,
        DefaultCharacterPath: AssetService.DefaultCharacterPath,
        DraggingCharacterPath: AssetService.DraggingCharacterPath,
        InputReactiveBasePath: AssetService.InputReactiveBasePath,
        Actions:
        [
            new PetActionDefinition(
                Id: "idle",
                Kind: PetActionKind.Idle,
                FramePaths: CreateFramePaths("States/Idle/Castorice.Idle", 8),
                FrameInterval: TimeSpan.FromMilliseconds(125)),
            new PetActionDefinition(
                Id: "move",
                Kind: PetActionKind.Move,
                FramePaths: CreateFramePaths("States/Move/Castorice.Move", 8),
                DistancePerFrame: 10,
                BaseSpeedPixelsPerSecond: 90,
                MinSpeedPixelsPerSecond: 80,
                MaxSpeedPixelsPerSecond: 105),
            new PetActionDefinition(
                Id: "blink",
                Kind: PetActionKind.Blink,
                FramePaths: CreateFramePaths("States/Blink/Castorice.Blink", 3),
                FrameInterval: TimeSpan.FromMilliseconds(90),
                MinScheduleDelay: TimeSpan.FromSeconds(3),
                MaxScheduleDelay: TimeSpan.FromSeconds(7)),
            new PetActionDefinition(
                Id: "petting",
                Kind: PetActionKind.Petting,
                FramePaths: CreateFramePaths("States/Petting/Castorice.Petting", 8),
                FrameInterval: TimeSpan.FromMilliseconds(80)),
            new PetActionDefinition(
                Id: "expression-transition-in",
                Kind: PetActionKind.ExpressionTransitionIn,
                FramePaths: CreateFramePaths("Expressions/Transition/Castorice.ExpressionTransition.In", 4),
                FrameInterval: TimeSpan.FromMilliseconds(55)),
            new PetActionDefinition(
                Id: "expression-transition-out",
                Kind: PetActionKind.ExpressionTransitionOut,
                FramePaths: CreateFramePaths("Expressions/Transition/Castorice.ExpressionTransition.Out", 4),
                FrameInterval: TimeSpan.FromMilliseconds(55)),
        ],
        Expressions:
        [
            CreateExpression("happy", "Happy"),
            CreateExpression("shy", "Shy"),
            CreateExpression("sleepy", "Sleepy"),
            CreateExpression("surprised", "Surprised"),
            CreateExpression("pouting", "Pouting"),
            CreateExpression("confused", "Confused"),
            CreateExpression("proud", "Proud"),
            CreateExpression("crying", "Crying"),
        ]);

    private static PetExpressionDefinition CreateExpression(string id, string label)
    {
        var transitionFrames = Enumerable
            .Range(0, 6)
            .Select(index => $"{CastoriceRuntimeRoot}/Expressions/{label}/Transition/Castorice.Expression.{label}.Transition.{index:00}.png")
            .ToArray();
        return new PetExpressionDefinition(
            id,
            label,
            $"{CastoriceRuntimeRoot}/Expressions/Castorice.Expression.{label}.png",
            transitionFrames,
            TimeSpan.FromMilliseconds(1000d / 15d));
    }

    private static IReadOnlyList<string> CreateFramePaths(string prefix, int count)
    {
        return Enumerable
            .Range(0, count)
            .Select(index => $"{CastoriceRuntimeRoot}/{prefix}.{index:00}.png")
            .ToArray();
    }
}
