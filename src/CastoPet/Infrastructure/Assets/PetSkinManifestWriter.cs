using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using CastoPet.Core.Animation;
using CastoPet.Core.Skins;

namespace CastoPet.Infrastructure.Assets;

public static class PetSkinManifestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void WriteToFile(string manifestPath, PetSkinDefinition skin)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(manifestPath, ToJson(skin));
    }

    public static string ToJson(PetSkinDefinition skin)
    {
        var pathRoot = skin.SourceResourceDirectory ?? skin.ResourceRoot;
        var manifest = new SkinManifest(
            SchemaVersion: 3,
            Id: skin.Id,
            DisplayName: skin.DisplayName,
            ResourceRoot: skin.ResourceRoot,
            DefaultCharacter: ToManifestPath(skin.DefaultCharacterPath, pathRoot),
            DraggingCharacter: ToOptionalManifestPath(skin.DraggingCharacterPath, pathRoot),
            InputReactiveBase: ToOptionalManifestPath(skin.InputReactiveBasePath, pathRoot),
            Actions: skin.Actions.Select(action => new ActionManifest(
                Id: action.Id,
                Kind: ToManifestKind(action.Kind),
                Frames: action.FramePaths.Select(path => ToManifestPath(path, pathRoot)).ToArray(),
                FrameIntervalMs: ToMilliseconds(action.FrameInterval),
                FrameDurationsMs: action.FrameDurations is { Count: > 0 }
                    ? action.FrameDurations.Select(ToMilliseconds).ToArray()
                    : null,
                MinScheduleDelayMs: ToMilliseconds(action.MinScheduleDelay),
                MaxScheduleDelayMs: ToMilliseconds(action.MaxScheduleDelay),
                Movement: action.Movement is { } movement ? new MovementManifest(
                    LeftFrames: movement.LeftFramePaths.Select(path => ToManifestPath(path, pathRoot)).ToArray(),
                    RightFrames: movement.RightFramePaths.Select(path => ToManifestPath(path, pathRoot)).ToArray(),
                    DistancePerFrame: movement.Settings.DistancePerFrame,
                    BaseSpeedPixelsPerSecond: movement.Settings.BaseSpeedPixelsPerSecond,
                    MinSpeedPixelsPerSecond: movement.Settings.MinSpeedPixelsPerSecond,
                    MaxSpeedPixelsPerSecond: movement.Settings.MaxSpeedPixelsPerSecond) : null)).ToArray(),
            Expressions: skin.Expressions.ToDictionary(
                item => item.Label,
                item => new ExpressionManifest(
                    Image: ToManifestPath(item.ResourcePath, pathRoot),
                    TransitionFrames: item.TransitionFramePaths is { Count: > 0 }
                        ? item.TransitionFramePaths.Select(path => ToManifestPath(path, pathRoot)).ToArray()
                        : null,
                    TransitionFrameIntervalMs: ToMilliseconds(item.TransitionFrameInterval)),
                StringComparer.OrdinalIgnoreCase));

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static string? ToOptionalManifestPath(string path, string resourceRoot)
    {
        return string.IsNullOrWhiteSpace(path) ? null : ToManifestPath(path, resourceRoot);
    }

    private static string ToManifestPath(string path, string resourceRoot)
    {
        var normalizedPath = path.Replace('\\', '/').Trim('/');
        var normalizedRoot = resourceRoot.Replace('\\', '/').Trim('/');

        if (string.IsNullOrEmpty(normalizedRoot))
        {
            return normalizedPath;
        }

        return normalizedPath.StartsWith($"{normalizedRoot}/", StringComparison.OrdinalIgnoreCase)
            ? normalizedPath[(normalizedRoot.Length + 1)..]
            : normalizedPath;
    }

    private static double? ToMilliseconds(TimeSpan? value)
    {
        return value?.TotalMilliseconds;
    }

    private static string ToManifestKind(PetActionKind kind)
    {
        return kind switch
        {
            PetActionKind.Idle => "idle",
            PetActionKind.Move => "move",
            PetActionKind.Blink => "blink",
            PetActionKind.Petting => "petting",
            PetActionKind.ExpressionTransitionIn => "expression-transition-in",
            PetActionKind.ExpressionTransitionOut => "expression-transition-out",
            _ => throw new InvalidOperationException($"Unsupported action kind {kind}."),
        };
    }

    private sealed record SkinManifest(
        int SchemaVersion,
        string Id,
        string DisplayName,
        string ResourceRoot,
        string DefaultCharacter,
        string? DraggingCharacter,
        string? InputReactiveBase,
        IReadOnlyList<ActionManifest> Actions,
        IReadOnlyDictionary<string, ExpressionManifest> Expressions);

    private sealed record ExpressionManifest(
        string Image,
        IReadOnlyList<string>? TransitionFrames,
        double? TransitionFrameIntervalMs);

    private sealed record ActionManifest(
        string Id,
        string Kind,
        IReadOnlyList<string> Frames,
        double? FrameIntervalMs,
        IReadOnlyList<double?>? FrameDurationsMs,
        double? MinScheduleDelayMs,
        double? MaxScheduleDelayMs,
        MovementManifest? Movement);

    private sealed record MovementManifest(
        IReadOnlyList<string> LeftFrames,
        IReadOnlyList<string> RightFrames,
        double DistancePerFrame,
        double BaseSpeedPixelsPerSecond,
        double MinSpeedPixelsPerSecond,
        double MaxSpeedPixelsPerSecond);
}
