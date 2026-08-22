namespace CastoPet.Core.Skins;

public sealed record PetExpressionDefinition(
    string Id,
    string Label,
    string ResourcePath,
    IReadOnlyList<string>? TransitionFramePaths = null,
    TimeSpan? TransitionFrameInterval = null);
