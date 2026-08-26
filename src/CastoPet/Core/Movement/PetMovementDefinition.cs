namespace CastoPet.Core.Movement;

public sealed record PetMovementDefinition(
    PetMovementSettings Settings,
    IReadOnlyList<string> LeftFramePaths,
    IReadOnlyList<string> RightFramePaths)
{
    public IReadOnlyList<string> GetDirectionalFramePaths(PetHorizontalDirection direction) =>
        direction switch
        {
            PetHorizontalDirection.Left => LeftFramePaths,
            PetHorizontalDirection.Right => RightFramePaths,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
}
