using System.IO;
using System.Text;
using System.Text.Json;

using CastoPet.Core.Animation;
using CastoPet.Core.Movement;
using CastoPet.Core.Skins;

namespace CastoPet.Infrastructure.Assets;

public static class PetSkinManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 16,
    };

    public static PetSkinDefinition LoadFromJson(string json)
    {
        var manifest = Deserialize(json);
        return BuildSkin(manifest, PathResolver.ForResourcePaths(manifest.ResourceRoot));
    }

    public static PetSkinDefinition LoadFromFile(string manifestPath)
    {
        var fullManifestPath = GetExternalManifestPath(manifestPath);
        var manifestDirectory = Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidDataException("Manifest path must have a directory.");
        if (!File.Exists(fullManifestPath))
        {
            throw new InvalidDataException($"External skin manifest does not exist: {fullManifestPath}.");
        }

        var volumeRoot = Path.GetPathRoot(fullManifestPath)
            ?? throw new InvalidDataException("External skin manifest path must have a volume root.");
        if (ExternalSkinPathPolicy.ContainsReparsePoint(volumeRoot, fullManifestPath))
        {
            throw new InvalidDataException(
                "External skin manifest path cannot contain a symbolic link or junction.");
        }

        var manifest = Deserialize(ReadExternalManifest(fullManifestPath));

        return BuildSkin(manifest, PathResolver.ForFilePaths(manifestDirectory, manifest.ResourceRoot));
    }

    private static PetSkinDefinition BuildSkin(SkinManifest manifest, PathResolver resolver)
    {
        ValidateManifest(manifest);

        var manifestActions = manifest.Actions!;
        ValidateActionIdentities(manifestActions);
        var left = manifestActions.SingleOrDefault(action => NormalizeKind(action.Kind!) == "move-left");
        var right = manifestActions.SingleOrDefault(action => NormalizeKind(action.Kind!) == "move-right");
        if (manifest.SchemaVersion == 3 && manifestActions.Any(action => IsLegacyKind(action.Kind!)))
        {
            throw new InvalidDataException("Schema 3 uses one move action with directional frames and does not support turn actions.");
        }

        // Retired turns are filtered before resolving paths, so their PNGs may be removed.
        var actions = manifestActions
            .Where(action => !IsLegacyKind(action.Kind!))
            .Select(action => BuildAction(action, resolver, manifest.SchemaVersion, left, right))
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
            DefaultCharacterPath: resolver.Resolve(RequiredPath(manifest.DefaultCharacter, "defaultCharacter")),
            DraggingCharacterPath: resolver.ResolveOptional(manifest.DraggingCharacter),
            InputReactiveBasePath: resolver.ResolveOptional(manifest.InputReactiveBase),
            Actions: actions,
            Expressions: expressions)
        {
            SourceResourceDirectory = resolver.SourceResourceDirectory,
        };
    }

    private static SkinManifest Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Manifest is empty.");
        }

        if (Encoding.UTF8.GetByteCount(json) > ExternalSkinResourceLimits.MaxManifestBytes)
        {
            throw new InvalidDataException(
                $"Skin manifest exceeds the {ExternalSkinResourceLimits.MaxManifestBytes}-byte limit.");
        }

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
        if (manifest.SchemaVersion is not (1 or 2 or 3))
        {
            throw new InvalidDataException($"Unsupported skin manifest schemaVersion {manifest.SchemaVersion}.");
        }

        _ = RequiredText(manifest.Id, "id");
        _ = RequiredText(manifest.DisplayName, "displayName");
        _ = RequiredPath(manifest.DefaultCharacter, "defaultCharacter");

        if (manifest.ResourceRoot is { Length: > ExternalSkinResourceLimits.MaxPathCharacters })
        {
            throw new InvalidDataException("Manifest resourceRoot is too long.");
        }

        if (manifest.Actions is null || manifest.Actions.Count == 0)
        {
            throw new InvalidDataException("Manifest must define actions.");
        }

        if (manifest.Actions.Count > ExternalSkinResourceLimits.MaxActions)
        {
            throw new InvalidDataException(
                $"Manifest actions exceed the {ExternalSkinResourceLimits.MaxActions}-action limit.");
        }

        if (manifest.Expressions?.Count > ExternalSkinResourceLimits.MaxExpressions)
        {
            throw new InvalidDataException(
                $"Manifest expressions exceed the {ExternalSkinResourceLimits.MaxExpressions}-expression limit.");
        }

        ValidateResourceReferenceBudget(manifest);
    }

    private static PetExpressionDefinition BuildExpression(string label, JsonElement value, PathResolver resolver)
    {
        _ = RequiredText(label, "expression label");
        if (value.ValueKind == JsonValueKind.String)
        {
            return new PetExpressionDefinition(
                Id: ToExpressionId(label),
                Label: label,
                ResourcePath: resolver.Resolve(RequiredPath(value.GetString(), $"expression.{label}")));
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Expression {label} must be a path string or object.");
        }

        var manifest = DeserializeExpression(label, value);
        if (manifest.TransitionFrames?.Count > ExternalSkinResourceLimits.MaxExpressionTransitionFrames)
        {
            throw new InvalidDataException(
                $"Expression {label} transitionFrames exceed the "
                + $"{ExternalSkinResourceLimits.MaxExpressionTransitionFrames}-frame limit.");
        }

        ValidatePositive(
            manifest.TransitionFrameIntervalMs,
            $"expression {label} transitionFrameIntervalMs");
        return new PetExpressionDefinition(
            Id: ToExpressionId(label),
            Label: label,
            ResourcePath: resolver.Resolve(RequiredPath(manifest.Image, $"expression.{label}.image")),
            TransitionFramePaths: manifest.TransitionFrames?
                .Select((path, index) => resolver.Resolve(RequiredPath(
                    path,
                    $"expression.{label}.transitionFrames[{index}]")))
                .ToArray(),
            TransitionFrameInterval: MillisecondsToTimeSpan(manifest.TransitionFrameIntervalMs));
    }

    private static PetActionDefinition BuildAction(
        ActionManifest manifest,
        PathResolver resolver,
        int schemaVersion,
        ActionManifest? left,
        ActionManifest? right)
    {
        var id = RequiredText(manifest.Id, "action.id");
        var kind = ParseActionKind(RequiredText(manifest.Kind, $"action {id}.kind"));
        var isMovement = kind == PetActionKind.Move;
        ValidateActionMetadata(manifest, id, allowEmptyFrames: isMovement && schemaVersion == 3);
        if (schemaVersion < 3 && manifest.Movement is not null)
        {
            throw new InvalidDataException($"Action {id} movement requires schemaVersion 3.");
        }

        if (!isMovement && manifest.Movement is not null)
        {
            throw new InvalidDataException($"Action {id} cannot define movement settings.");
        }

        if (schemaVersion == 3 && (manifest.DistancePerFrame is not null || manifest.BaseSpeedPixelsPerSecond is not null
            || manifest.MinSpeedPixelsPerSecond is not null || manifest.MaxSpeedPixelsPerSecond is not null))
        {
            throw new InvalidDataException($"Action {id} must place shared movement settings inside movement.");
        }

        var frames = (manifest.Frames ?? [])
            .Select((path, index) => resolver.Resolve(RequiredPath(path, $"action {id}.frames[{index}]")))
            .ToArray();

        return new PetActionDefinition(
            Id: id,
            Kind: kind,
            FramePaths: frames,
            FrameInterval: isMovement ? null : MillisecondsToTimeSpan(manifest.FrameIntervalMs),
            FrameDurations: isMovement ? null : manifest.FrameDurationsMs?
                .Select(MillisecondsToTimeSpan)
                .ToArray(),
            MinScheduleDelay: isMovement ? null : MillisecondsToTimeSpan(manifest.MinScheduleDelayMs),
            MaxScheduleDelay: isMovement ? null : MillisecondsToTimeSpan(manifest.MaxScheduleDelayMs),
            Movement: isMovement ? BuildMovement(manifest, resolver, schemaVersion, left, right) : null);
    }

    private static PetMovementDefinition BuildMovement(
        ActionManifest action, PathResolver resolver, int schemaVersion, ActionManifest? left, ActionManifest? right)
    {
        var movement = action.Movement;
        if (schemaVersion == 3 && movement is null)
        {
            throw new InvalidDataException("Schema 3 move action must define movement.");
        }

        if (schemaVersion == 3 && (action.FrameIntervalMs is not null || action.FrameDurationsMs is not null
            || action.MinScheduleDelayMs is not null || action.MaxScheduleDelayMs is not null))
        {
            throw new InvalidDataException("Movement is distance-driven and cannot define frame timing or schedule delays.");
        }

        var defaults = new PetMovementSettings();
        var settings = new PetMovementSettings(
            movement?.DistancePerFrame ?? action.DistancePerFrame ?? defaults.DistancePerFrame,
            movement?.BaseSpeedPixelsPerSecond ?? action.BaseSpeedPixelsPerSecond ?? defaults.BaseSpeedPixelsPerSecond,
            movement?.MinSpeedPixelsPerSecond ?? action.MinSpeedPixelsPerSecond ?? defaults.MinSpeedPixelsPerSecond,
            movement?.MaxSpeedPixelsPerSecond ?? action.MaxSpeedPixelsPerSecond ?? defaults.MaxSpeedPixelsPerSecond);
        try
        {
            settings.Validate();
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"Invalid movement settings for {action.Id}: {ex.Message}", ex);
        }

        ValidateLegacyDirectionalSettings(left, settings);
        ValidateLegacyDirectionalSettings(right, settings);
        var leftPaths = ResolveMovementFrames(movement?.LeftFrames ?? left?.Frames, "leftFrames", resolver);
        var rightPaths = ResolveMovementFrames(movement?.RightFrames ?? right?.Frames, "rightFrames", resolver);
        if (action.Frames is not { Count: > 0 } && (leftPaths.Count == 0 || rightPaths.Count == 0))
        {
            throw new InvalidDataException("Movement needs both directional clips or generic fallback frames.");
        }

        return new PetMovementDefinition(settings, leftPaths, rightPaths);
    }

    private static IReadOnlyList<string> ResolveMovementFrames(List<string>? frames, string label, PathResolver resolver)
    {
        if (frames?.Count > ExternalSkinResourceLimits.MaxFramesPerAction)
        {
            throw new InvalidDataException($"Movement {label} exceeds the frame limit.");
        }

        return (frames ?? []).Select((path, index) => resolver.Resolve(RequiredPath(path, $"movement.{label}[{index}]"))).ToArray();
    }

    private static void ValidateLegacyDirectionalSettings(ActionManifest? action, PetMovementSettings shared)
    {
        if (action is null)
        {
            return;
        }

        ValidateActionMetadata(action, action.Id!);
        foreach (var (name, value, effective) in new (string, double?, double)[]
        {
            ("distancePerFrame", action.DistancePerFrame, shared.DistancePerFrame),
            ("baseSpeedPixelsPerSecond", action.BaseSpeedPixelsPerSecond, shared.BaseSpeedPixelsPerSecond),
            ("minSpeedPixelsPerSecond", action.MinSpeedPixelsPerSecond, shared.MinSpeedPixelsPerSecond),
            ("maxSpeedPixelsPerSecond", action.MaxSpeedPixelsPerSecond, shared.MaxSpeedPixelsPerSecond),
        })
        {
            if (value is double number && number != effective)
            {
                throw new InvalidDataException($"Legacy action {action.Id} {name} conflicts with shared move settings ({number} vs {effective}).");
            }
        }
    }

    private static string NormalizeKind(string kind) => kind.ToLowerInvariant() switch
    {
        "moveleft" => "move-left",
        "moveright" => "move-right",
        "turnleft" => "turn-left",
        "turnright" => "turn-right",
        _ => kind.ToLowerInvariant(),
    };

    private static bool IsLegacyKind(string kind) => NormalizeKind(kind) is "move-left" or "move-right" or "turn-left" or "turn-right";

    private static void ValidateActionIdentities(IReadOnlyList<ActionManifest> actions)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in actions)
        {
            if (action is null)
            {
                throw new InvalidDataException("Manifest action must not be null.");
            }

            var id = RequiredText(action.Id, "action.id");
            var kind = NormalizeKind(RequiredText(action.Kind, $"action {id}.kind"));
            if (!ids.Add(id))
            {
                throw new InvalidDataException($"Duplicate action id {id}.");
            }

            if (!kinds.Add(kind))
            {
                throw new InvalidDataException($"Duplicate action kind {kind}.");
            }
        }
    }

    private static void ValidateActionMetadata(ActionManifest manifest, string id, bool allowEmptyFrames = false)
    {
        if (!allowEmptyFrames && (manifest.Frames is null || manifest.Frames.Count == 0))
        {
            throw new InvalidDataException($"Action {id} must define at least one frame.");
        }

        if (manifest.Frames?.Count > ExternalSkinResourceLimits.MaxFramesPerAction)
        {
            throw new InvalidDataException(
                $"Action {id} frames exceed the {ExternalSkinResourceLimits.MaxFramesPerAction}-frame limit.");
        }

        ValidatePositive(manifest.FrameIntervalMs, $"action {id} frameIntervalMs");
        if (manifest.FrameDurationsMs is { } frameDurations)
        {
            if (frameDurations.Count != (manifest.Frames?.Count ?? 0))
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
            _ when value.Equals("blink", StringComparison.OrdinalIgnoreCase) => PetActionKind.Blink,
            _ when value.Equals("petting", StringComparison.OrdinalIgnoreCase) => PetActionKind.Petting,
            _ when value.Equals("expressionTransitionIn", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionIn,
            _ when value.Equals("expression-transition-in", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionIn,
            _ when value.Equals("expressionTransitionOut", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionOut,
            _ when value.Equals("expression-transition-out", StringComparison.OrdinalIgnoreCase) => PetActionKind.ExpressionTransitionOut,
            _ => throw new InvalidDataException($"Unsupported action kind {value}."),
        };
    }

    private static string RequiredText(
        string? value,
        string name,
        int maxCharacters = ExternalSkinResourceLimits.MaxTextCharacters)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Manifest must define {name}.");
        }

        if (value.Length > maxCharacters)
        {
            throw new InvalidDataException($"Manifest {name} is too long.");
        }

        return value;
    }

    private static string RequiredPath(string? value, string name) =>
        RequiredText(value, name, ExternalSkinResourceLimits.MaxPathCharacters);

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

    private static void ValidateResourceReferenceBudget(SkinManifest manifest)
    {
        var references = 1;
        if (!string.IsNullOrWhiteSpace(manifest.DraggingCharacter))
        {
            references++;
        }

        if (!string.IsNullOrWhiteSpace(manifest.InputReactiveBase))
        {
            references++;
        }

        foreach (var action in manifest.Actions!)
        {
            if (action is null)
            {
                throw new InvalidDataException("Manifest action must not be null.");
            }

            references += action.Frames?.Count ?? 0;
            references += action.Movement?.LeftFrames?.Count ?? 0;
            references += action.Movement?.RightFrames?.Count ?? 0;
        }

        if (manifest.Expressions is not null)
        {
            foreach (var item in manifest.Expressions)
            {
                references++;
                if (item.Value.ValueKind == JsonValueKind.Object)
                {
                    references += DeserializeExpression(item.Key, item.Value).TransitionFrames?.Count ?? 0;
                }
            }
        }

        if (references > ExternalSkinResourceLimits.MaxTotalFrameReferences)
        {
            throw new InvalidDataException(
                $"Manifest image references exceed the "
                + $"{ExternalSkinResourceLimits.MaxTotalFrameReferences}-resource limit.");
        }
    }

    private static ExpressionManifest DeserializeExpression(string label, JsonElement value)
    {
        try
        {
            return value.Deserialize<ExpressionManifest>(JsonOptions)
                ?? throw new InvalidDataException($"Expression {label} is invalid.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Expression {label} is invalid.", ex);
        }
    }

    private static string GetExternalManifestPath(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new InvalidDataException("External skin manifest path is required.");
        }

        if (ExternalSkinPathPolicy.IsUncPath(manifestPath))
        {
            throw new InvalidDataException("External skin manifest cannot use a UNC path.");
        }

        try
        {
            var fullPath = Path.GetFullPath(manifestPath);
            if (ExternalSkinPathPolicy.IsUncPath(fullPath))
            {
                throw new InvalidDataException("External skin manifest cannot use a UNC path.");
            }

            return fullPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDataException("External skin manifest path is invalid.", ex);
        }
    }

    private static string ReadExternalManifest(string manifestPath)
    {
        using var stream = new FileStream(
            manifestPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        if (stream.Length <= 0 || stream.Length > ExternalSkinResourceLimits.MaxManifestBytes)
        {
            throw new InvalidDataException(
                $"External skin manifest must contain 1 to "
                + $"{ExternalSkinResourceLimits.MaxManifestBytes} bytes.");
        }

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);
        return reader.ReadToEnd();
    }

    private sealed class PathResolver
    {
        private readonly string _root;
        private readonly bool _fileSystemPaths;

        public string? SourceResourceDirectory => _fileSystemPaths ? _root : null;

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
            var root = ExternalSkinPathPolicy.ResolveResourceRoot(manifestDirectory, resourceRoot);
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
                return ExternalSkinPathPolicy.ResolvePng(_root, relativePath);
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
        public MovementManifest? Movement { get; set; }
    }

    private sealed class MovementManifest
    {
        public List<string>? LeftFrames { get; set; }
        public List<string>? RightFrames { get; set; }
        public double? DistancePerFrame { get; set; }
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
