namespace CastoPet.Core;

public static class ExternalSkinResourceLimits
{
    public const long MaxManifestBytes = 512 * 1024;
    public const int MaxActions = 32;
    public const int MaxFramesPerAction = 120;
    public const int MaxTotalFrameReferences = 512;
    public const int MaxExpressions = 32;
    public const int MaxExpressionTransitionFrames = 30;
    public const long MaxImageFileBytes = 16 * 1024 * 1024;
    public const int MaxImageDimension = 4096;
    public const long MaxImagePixels = 16 * 1024 * 1024;
    public const double MaxImageAspectRatio = 4;
    public const int MaxTextCharacters = 128;
    public const int MaxPathCharacters = 1024;
}
