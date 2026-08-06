using System.IO;
using System.Text.Json;

namespace CastoPet.Core;

public static class PetSkinManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static PetSkinDefinition LoadFromJson(string json)
    {
        var manifest = Deserialize(json);
        return BuildSkin(manifest, PathResolver.ForResourcePaths(manifest.ResourceRoot));
    }

    public static PetSkinDefinition LoadFromFile(string manifestPath)
    {
        var fullManifestPath = Path.GetFullPath(manifestPath);
        var manifest = Deserialize(File.ReadAllText(fullManifestPath));
        var manifestDirectory = Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidDataException("Manifest path must have a directory.");

        return BuildSkin(manifest, PathResolver.ForFilePaths(manifestDirectory, manifest.ResourceRoot));
    }

    private static PetSkinDefinition BuildSkin(SkinManifest manifest, PathResolver resolver)
    {
        ValidateManifest(manifest);

        var actions = manifest.Actions!
            .Select(action => BuildAction(action, resolver))
            .ToArray();

        ValidateActions(actions);
        RequireAction(actions, PetActionKind.Idle);
        RequireAction(actions, PetActionKind.Move);
        RequireAction(actions, PetActionKind.Blink);

        var expressions = new List<PetExpressionDefinition>();
        if (manifest.Expressions is not null)
        {
            foreach (var item in manifest.Expressions)
            {
                expressions.Add(BuildExpression(item.Key, item.Value, resolver));
            }
        }

        ValidateExpressions(expressions);

        return new PetSkinDefinition(
            Id: RequiredText(manifest.Id, "id"),
            DisplayName: RequiredText(manifest.DisplayName, "displayName"),
            ResourceRoot: manifest.ResourceRoot ?? string.Empty,
            DefaultCharacterPath: resolver.Resolve(RequiredText(manifest.DefaultCharacter, "defaultCharacter")),
            DraggingCharacterPath: resolver.ResolveOptional(manifest.DraggingCharacter),
            InputReactiveBasePath: resolver.ResolveOptional(manifest.InputReactiveBase),
            Actions: actions,
            Expressions: expressions);
    }

    private static SkinManifest Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SkinManifest>(json, JsonOptions)
                ?? throw new InvalidDataException("Manifest is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Manifest JSON is invalid.", ex);
        }
    }

    private static void ValidateManifest(SkinManifest manifest)
    {
        if (manifest.SchemaVersion is not (1 or 2))
        {
            throw new InvalidDataException($"Unsupported skin manifest schemaVersion {manifest.SchemaVersion}.");
        }

        _ = RequiredText(manifest.Id, "id");
        _ = RequiredText(manifest.DisplayName, "displayName");
        _ = RequiredText(manifest.DefaultCharacter, "defaultCharacter");

        if (manifest.Actions is null || manifest.Actions.Count == 0)
        {
            throw new InvalidDataException("Manifest must define actions.");
        }
    }

    private static PetExpressionDefinition BuildExpression(string label, JsonElement value, PathResolver resolver)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return new PetExpressionDefinition(
                Id: ToExpressionId(label),
                Label: label,
                ResourcePath: resolver.Resolve(RequiredText(value.GetString(), $"expression.{label}")));
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Expression {label} must be a path string or object.");
        }

        var manifest = value.Deserialize<ExpressionManifest>(JsonOptions)
            ?? throw new InvalidDataException($"Expression {label} is invalid.");
        ValidatePositive(
            manifest.TransitionFrameIntervalMs,
            $"expression {label} transitionFrameIntervalMs");
        return new PetExpressionDefinition(
            Id: ToExpressionId(label),
            Label: label,
            ResourcePath: resolver.Resolve(RequiredText(manifest.Image, $"expression.{label}.image")),
            TransitionFramePaths: manifest.TransitionFrames?
                .Select((path, index) => resolver.Resolve(RequiredText(
                    path,
                    $"expression.{label}.transitionFrames[{index}]")))
                .ToArray(),
            TransitionFrameInterval: MillisecondsToTimeSpan(manifest.TransitionFrameIntervalMs));
    }

    private static PetActionDefinition BuildAction(ActionManifest manifest, PathResolver resolver)
    {
        var id = RequiredText(manifest.Id, "action.id");
        var kind = ParseActionKind(RequiredText(manifest.Kind, $"action {id}.kind"));
        ValidateActionMetadata(manifest, id);
        var frames = manifest.Frames!
            .Select((path, index) => resolver.Resolve(RequiredText(path, $"action {id}.frames[{index}]")))
            .ToArray();

        return new PetActionDefinition(
            Id: id,
            Kind: kind,
            FramePaths: frames,
            FrameInterval: MillisecondsToTimeSpan(manifest.FrameIntervalMs),
            FrameDurations: manifest.FrameDurationsMs?
                .Select(MillisecondsToTimeSpan)
                .ToArray(),
            DistancePerFrame: manifest.DistancePerFrame,
            MinScheduleDelay: MillisecondsToTimeSpan(manifest.MinScheduleDelayMs),
            MaxScheduleDelay: MillisecondsToTimeSpan(manifest.MaxScheduleDelayMs),
            BaseSpeedPixelsPerSecond: manifest.BaseSpeedPixelsPerSecond,
            MinSpeedPixelsPerSecond: manifest.MinSpeedPixelsPerSecond,
            MaxSpeedPixelsPerSecond: manifest.MaxSpeedPixelsPerSecond);
    }

    private static void ValidateActionMetadata(ActionManifest manifest, string id)
    {
        if (manifest.Frames is null || manifest.Frames.Count == 0)
        {
            throw new InvalidDataException($"Action {id} must define at least one frame.");
        }

        ValidatePositive(manifest.FrameIntervalMs, $"action {id} frameIntervalMs");
        if (manifest.FrameDurationsMs is { } frameDurations)
        {
            if (frameDurations.Count != manifest.Frames.Count)
            {
                throw new InvalidDataException(
                    $"Action {id} frameDurationsMs must contain one entry per frame.");
            }

            for (var index = 0; index < frameDurations.Count; index++)
            {
                ValidatePositive(frameDurations[index], $"action {id} frameDurationsMs[{index}]");
            }
        }

        ValidatePositive(manifest.DistancePerFrame, $"action {id} distancePerFrame");
        ValidatePositive(manifest.MinScheduleDelayMs, $"action {id} minScheduleDelayMs");
        ValidatePositive(manifest.MaxScheduleDelayMs, $"action {id} maxScheduleDelayMs");
        ValidatePositive(manifest.BaseSpeedPixelsPerSecond, $"action {id} baseSpeedPixelsPerSecond");
        ValidatePositive(manifest.MinSpeedPixelsPerSecond, $"action {id} minSpeedPixelsPerSecond");
        ValidatePositive(manifest.MaxSpeedPixelsPerSecond, $"action {id} maxSpeedPixelsPerSecond");

        if (manifest.MinScheduleDelayMs is double minDelay
            && manifest.MaxScheduleDelayMs is double maxDelay
            && minDelay > maxDelay)
        {
            throw new InvalidDataException($"Action {id} has an invalid schedule delay range.");
        }

        if (manifest.MinSpeedPixelsPerSecond is double minSpeed
            && manifest.MaxSpeedPixelsPerSecond is double maxSpeed
            && minSpeed > maxSpeed)
        {
            throw new InvalidDataException($"Action {id} has an invalid speed range.");
        }

        if (manifest.BaseSpeedPixelsPerSecond is double baseSpeed)
        {
            if (manifest.MinSpeedPixelsPerSecond is double minimum && baseSpeed < minimum
                || manifest.MaxSpeedPixelsPerSecond is double maximum && baseSpeed > maximum)
            {
                throw new InvalidDataException($"Action {id} base speed must be within its speed range.");
            }
        }
    }

    private static void ValidateActions(IReadOnlyList<PetActionDefinition> actions)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kinds = new HashSet<PetActionKind>();
        foreach (var action in actions)
        {
            if (!ids.Add(action.Id))
            {
                throw new InvalidDataException($"Duplicate action id {action.Id}.");
            }

            if (!kinds.Add(action.Kind))
            {
                throw new InvalidDataException($"Duplicate action kind {action.Kind}.");
            }
        }
    }

    private static void ValidateExpressions(IReadOnlyList<PetExpressionDefinition> expressions)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var expression in expressions)
        {
            if (!ids.Add(expression.Id))
            {
                throw new InvalidDataException($"Duplicate expression id {expression.Id}.");
            }
        }
    }

    private static void ValidatePositive(double? value, string name)
    {
        if (value is double number && (!double.IsFinite(number) || number <= 0))
        {
            throw new InvalidDataException($"Manifest {name} must be greater than zero.");
        }
    }

    private static PetActionKind ParseActionKind(string value)
    {
        return value switch
        {
            _ when value.Equals("idle", StringComparison.OrdinalIgnoreCase) => PetActionKind.Idle,
            _ when value.Equals("move", StringComparison.OrdinalIgnoreCase) => PetActionKind.Move,
            _ when value.Equals("moveLeft", StringComparison.OrdinalIgnoreCase) => PetActionKind.MoveLeft,
            _ when value.Equals("move-left", StringComparison.OrdinalIgnoreCase) => PetActionKind.MoveLeft,
            _ when value.Equals("moveRight", StringComparison.OrdinalIgnoreCase) => PetActionKind.MoveRight,
            _ when value.Equals("move-right", StringComparison.OrdinalIgnoreCase) => PetActionKind.MoveRight,
            _ when value.Equals("turnLeft", StringComparison.OrdinalIgnoreCase) => PetActionKind.TurnLeft,
            _ when value.Equals("turn-left", StringComparison.OrdinalIgnoreCase) => PetActionKind.TurnLeft,
            _ when value.Equals("turnRight", StringComparison.OrdinalIgnoreCase) => PetActionKind.TurnRight,
            _ when value.Equals("turn-right", StringComparison.OrdinalIgnoreCase) => PetActionKind.TurnRight,
            _ when value.Equals("blink", StringComparison.OrdinalIgnoreCase) => PetActionKind.Blink,
            _ when value.Equals("petting", StringComparison.OrdinalIgnoreCase) => PetActionKind.Petting,
            _ when value.Equals("expressionTransitionIn", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionIn,
            _ when value.Equals("expression-transition-in", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionIn,
            _ when value.Equals("expressionTransitionOut", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionOut,
            _ when value.Equals("expression-transition-out", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionOut,
            _ => throw new InvalidDataException($"Unsupported action kind {value}."),
        };
    }

    private static string RequiredText(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Manifest must define {name}.");
        }

        return value;
    }

    private static void RequireAction(IReadOnlyList<PetActionDefinition> actions, PetActionKind kind)
    {
        if (!actions.Any(action => action.Kind == kind))
        {
            throw new InvalidDataException($"Missing required action {kind}.");
        }
    }

    private static TimeSpan? MillisecondsToTimeSpan(double? milliseconds)
    {
        return milliseconds is null ? null : TimeSpan.FromMilliseconds(milliseconds.Value);
    }

    private static string ToExpressionId(string label)
    {
        return label.Trim().Replace(' ', '-').ToLowerInvariant();
    }

    private sealed class PathResolver
    {
        private readonly string _root;
        private readonly bool _fileSystemPaths;

        private PathResolver(string root, bool fileSystemPaths)
        {
            _root = root;
            _fileSystemPaths = fileSystemPaths;
        }

        public static PathResolver ForResourcePaths(string? resourceRoot)
        {
            return new PathResolver(NormalizeResourcePath(resourceRoot ?? string.Empty), fileSystemPaths: false);
        }

        public static PathResolver ForFilePaths(string manifestDirectory, string? resourceRoot)
        {
            var root = Path.GetFullPath(Path.Combine(manifestDirectory, resourceRoot ?? string.Empty));
            return new PathResolver(root, fileSystemPaths: true);
        }

        public string ResolveOptional(string? relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath) ? string.Empty : Resolve(relativePath);
        }

        public string Resolve(string relativePath)
        {
            if (_fileSystemPaths)
            {
                return Path.GetFullPath(Path.Combine(_root, relativePath));
            }

            var path = NormalizeResourcePath(relativePath);
            return string.IsNullOrEmpty(_root) ? path : $"{_root}/{path}";
        }

        private static string NormalizeResourcePath(string path)
        {
            return path
                .Replace('\\', '/')
                .Trim('/');
        }
    }

    private sealed class SkinManifest
    {
        public int SchemaVersion { get; set; }
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? ResourceRoot { get; set; }
        public string? DefaultCharacter { get; set; }
        public string? DraggingCharacter { get; set; }
        public string? InputReactiveBase { get; set; }
        public List<ActionManifest>? Actions { get; set; }
        public Dictionary<string, JsonElement>? Expressions { get; set; }
    }

    private sealed class ActionManifest
    {
        public string? Id { get; set; }
        public string? Kind { get; set; }
        public List<string>? Frames { get; set; }
        public double? FrameIntervalMs { get; set; }
        public List<double?>? FrameDurationsMs { get; set; }
        public double? DistancePerFrame { get; set; }
        public double? MinScheduleDelayMs { get; set; }
        public double? MaxScheduleDelayMs { get; set; }
        public double? BaseSpeedPixelsPerSecond { get; set; }
        public double? MinSpeedPixelsPerSecond { get; set; }
        public double? MaxSpeedPixelsPerSecond { get; set; }
    }

    private sealed class ExpressionManifest
    {
        public string? Image { get; set; }
        public List<string>? TransitionFrames { get; set; }
        public double? TransitionFrameIntervalMs { get; set; }
    }
}
