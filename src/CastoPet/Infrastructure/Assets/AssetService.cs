using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

using CastoPet.Core.Animation;
using CastoPet.Core.Movement;
using CastoPet.Core.Skins;
using CastoPet.Infrastructure.Diagnostics;

namespace CastoPet.Infrastructure.Assets;

public sealed class AssetService
{
    public const int CharacterDecodePixelWidth = 320;

    private readonly LoggingService _logger;

    public AssetService(LoggingService logger)
        : this(logger, BuiltInPetSkins.Castorice)
    {
    }

    public AssetService(LoggingService logger, PetSkinDefinition skin)
    {
        _logger = logger;
        Skin = skin;
    }

    public PetSkinDefinition Skin { get; }

    public BitmapImage LoadDefaultCharacter()
    {
        return LoadCharacter(Skin.DefaultCharacterPath, "Default character");
    }

    public BitmapImage LoadDraggingCharacter()
    {
        return LoadCharacter(Skin.DraggingCharacterPath, "Dragging character");
    }

    public IReadOnlyList<ImageSource> LoadIdleFrames()
    {
        return LoadActionFrames(PetActionKind.Idle, "Idle frames");
    }

    public IReadOnlyList<ImageSource> LoadBlinkFrames()
    {
        return LoadActionFrames(PetActionKind.Blink, "Blink frames");
    }

    public IReadOnlyList<ImageSource> LoadMoveFrames(PetHorizontalDirection direction)
    {
        var action = Skin.GetRequiredAction(PetActionKind.Move);
        var paths = action.Movement?.GetDirectionalFramePaths(direction);
        if (paths is { Count: > 0 })
        {
            try
            {
                return paths.Select(path => (ImageSource)LoadCharacter(path, $"{direction} move frames")).ToArray();
            }
            catch
            {
                // Legacy skins can fall back when an optional directional clip cannot be decoded.
            }
        }

        return LoadOptionalActionFrames(PetActionKind.Move, "Move fallback frames");
    }

    public IReadOnlyList<ImageSource> LoadPettingFrames()
    {
        return LoadOptionalActionFrames(PetActionKind.Petting, "Petting frames");
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionInFrames()
    {
        return LoadOptionalActionFrames(PetActionKind.ExpressionTransitionIn, "Expression transition in frames");
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionOutFrames()
    {
        return LoadOptionalActionFrames(PetActionKind.ExpressionTransitionOut, "Expression transition out frames");
    }

    public IReadOnlyDictionary<string, PetExpressionAsset> LoadExpressionAssets()
    {
        var assets = new Dictionary<string, PetExpressionAsset>(StringComparer.OrdinalIgnoreCase);

        foreach (var expression in Skin.Expressions)
        {
            if (TryLoadExpressionAsset(expression.Id) is { } asset)
            {
                assets[expression.Id] = asset;
            }
        }

        return assets;
    }

    public PetExpressionAsset? TryLoadExpressionAsset(string expressionId)
    {
        var expression = Skin.Expressions.FirstOrDefault(item =>
            string.Equals(item.Id, expressionId, StringComparison.OrdinalIgnoreCase));
        if (expression is null)
        {
            return null;
        }

        try
        {
            var image = LoadCharacter(expression.ResourcePath, "Expression wheel images");
            var transitionFrames = LoadOptionalExpressionTransitionFrames(expression);
            return new PetExpressionAsset(expression, image, transitionFrames);
        }
        catch
        {
            return null;
        }
    }

    public static string FormatLoadFailureMessage(string resourceGroup, string resourcePath)
    {
        return $"Failed to load {resourceGroup}: {resourcePath}.";
    }

    private IReadOnlyList<ImageSource> LoadActionFrames(PetActionKind kind, string resourceGroup)
    {
        return Skin
            .GetRequiredAction(kind)
            .FramePaths
            .Select(path => LoadCharacter(path, resourceGroup))
            .ToArray();
    }

    private IReadOnlyList<ImageSource> LoadOptionalActionFrames(PetActionKind kind, string resourceGroup)
    {
        if (!Skin.TryGetAction(kind, out var action))
        {
            return Array.Empty<ImageSource>();
        }

        try
        {
            return action.FramePaths.Select(path => (ImageSource)LoadCharacter(path, resourceGroup)).ToArray();
        }
        catch
        {
            return Array.Empty<ImageSource>();
        }
    }

    private IReadOnlyList<ImageSource> LoadOptionalExpressionTransitionFrames(PetExpressionDefinition expression)
    {
        if (expression.TransitionFramePaths is not { Count: > 0 })
        {
            return Array.Empty<ImageSource>();
        }

        try
        {
            return expression.TransitionFramePaths
                .Select(path => (ImageSource)LoadCharacter(path, $"{expression.Label} transition frames"))
                .ToArray();
        }
        catch
        {
            return Array.Empty<ImageSource>();
        }
    }

    private BitmapImage LoadCharacter(string resourcePath, string resourceGroup)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = CharacterDecodePixelWidth;
            image.UriSource = CreateImageUri(resourcePath);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            _logger.Error(FormatLoadFailureMessage(resourceGroup, resourcePath), ex);
            throw;
        }
    }

    private static Uri CreateImageUri(string resourcePath)
    {
        return Path.IsPathFullyQualified(resourcePath)
            ? new Uri(resourcePath, UriKind.Absolute)
            : new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
    }
}
