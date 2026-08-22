using System.Windows.Media;

namespace CastoPet.Core;

public sealed record PetExpressionAsset(
    PetExpressionDefinition Definition,
    ImageSource Image,
    IReadOnlyList<ImageSource> TransitionFrames);
