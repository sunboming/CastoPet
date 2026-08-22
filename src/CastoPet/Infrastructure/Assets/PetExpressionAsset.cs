using System.Windows.Media;

using CastoPet.Core.Skins;

namespace CastoPet.Infrastructure.Assets;

public sealed record PetExpressionAsset(
    PetExpressionDefinition Definition,
    ImageSource Image,
    IReadOnlyList<ImageSource> TransitionFrames);
