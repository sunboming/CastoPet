using CastoPet.Core.Animation;

namespace CastoPet.Core.Skins;

public sealed record PetSkinDefinition(
    string Id,
    string DisplayName,
    string ResourceRoot,
    string DefaultCharacterPath,
    string DraggingCharacterPath,
    IReadOnlyList<PetActionDefinition> Actions,
    IReadOnlyList<PetExpressionDefinition> Expressions)
{
    // File-based skins retain their resolved root for relative-path manifest export.
    public string? SourceResourceDirectory { get; init; }

    public bool TryGetAction(PetActionKind kind, out PetActionDefinition action)
    {
        foreach (var item in Actions)
        {
            if (item.Kind == kind)
            {
                action = item;
                return true;
            }
        }

        action = null!;
        return false;
    }

    public PetActionDefinition GetRequiredAction(PetActionKind kind)
    {
        return TryGetAction(kind, out var action)
            ? action
            : throw new InvalidOperationException($"Skin {Id} does not define action {kind}.");
    }
}
