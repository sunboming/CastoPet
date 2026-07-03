namespace CastoPet.Core;

public static class BuiltInPetSkins
{
    public static readonly PetSkinDefinition Castorice = new(
        Id: "castorice",
        DisplayName: "Castorice",
        ResourceRoot: "Assets",
        DefaultCharacterPath: AssetService.DefaultCharacterPath,
        DraggingCharacterPath: AssetService.DraggingCharacterPath,
        InputReactiveBasePath: AssetService.InputReactiveBasePath,
        Actions:
        [
            new PetActionDefinition(
                Id: "idle",
                Kind: PetActionKind.Idle,
                FramePaths: IdleFrameSequence.FramePaths,
                FrameInterval: IdleFrameSequence.FrameInterval),
            new PetActionDefinition(
                Id: "move",
                Kind: PetActionKind.Move,
                FramePaths: MoveFrameSequence.FramePaths,
                DistancePerFrame: MoveFrameSequence.DistancePerFrame,
                BaseSpeedPixelsPerSecond: MoveFrameSequence.BaseSpeedPixelsPerSecond,
                MinSpeedPixelsPerSecond: MoveFrameSequence.MinSpeedPixelsPerSecond,
                MaxSpeedPixelsPerSecond: MoveFrameSequence.MaxSpeedPixelsPerSecond),
            new PetActionDefinition(
                Id: "blink",
                Kind: PetActionKind.Blink,
                FramePaths: BlinkFrameSequence.FramePaths,
                FrameInterval: BlinkFrameSequence.FrameInterval,
                MinScheduleDelay: BlinkFrameSequence.MinScheduleDelay,
                MaxScheduleDelay: BlinkFrameSequence.MaxScheduleDelay),
            new PetActionDefinition(
                Id: "expression-transition-in",
                Kind: PetActionKind.ExpressionTransitionIn,
                FramePaths: ExpressionTransitionSequence.InFramePaths,
                FrameInterval: ExpressionTransitionSequence.FrameInterval),
            new PetActionDefinition(
                Id: "expression-transition-out",
                Kind: PetActionKind.ExpressionTransitionOut,
                FramePaths: ExpressionTransitionSequence.OutFramePaths,
                FrameInterval: ExpressionTransitionSequence.FrameInterval),
        ],
        Expressions: ExpressionWheelCatalog.Items.ToDictionary(
            item => item.Label,
            item => item.ResourcePath,
            StringComparer.OrdinalIgnoreCase));
}
