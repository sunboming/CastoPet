namespace CastoPet.Core.Movement;

public readonly record struct PetMovementStep(
    double NextLeft,
    double NextTop,
    double DeltaX,
    double DeltaY,
    double Distance);

public readonly record struct PetMoveFrameAdvance(int FrameIndex, bool Changed);

public sealed class PetMovementController
{
    private const double WanderRange = 160;

    private readonly PetMovementSettings _settings;
    private readonly Random _random;
    private TimeSpan? _lastRenderTime;
    private DateTime _nextWanderDecisionUtc = DateTime.MinValue;
    private double _logicalLeft;
    private double _logicalTop;
    private double _frameDistanceAccumulator;

    public PetMovementController(PetMovementSettings settings, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        _settings = settings;
        _random = random ?? new Random();
    }

    public bool HasTarget { get; private set; }
    public bool HasRenderingTime => _lastRenderTime is not null;
    public PetMovementTarget Target { get; private set; }
    public double LastDeltaX { get; private set; }
    public int MoveFrameIndex { get; private set; }

    public void BeginRendering(double left, double top)
    {
        _logicalLeft = left;
        _logicalTop = top;
        _lastRenderTime = null;
    }

    public void StopRendering()
    {
        _lastRenderTime = null;
    }

    public void SetTarget(PetMovementTarget target)
    {
        Target = target;
        HasTarget = true;
    }

    public void CancelTarget()
    {
        HasTarget = false;
    }

    public void CompleteTarget(DateTime nowUtc)
    {
        HasTarget = false;
        _nextWanderDecisionUtc = nowUtc.AddMilliseconds(_random.Next(1200, 2600));
    }

    public bool IsWanderDue(DateTime nowUtc)
    {
        return nowUtc >= _nextWanderDecisionUtc;
    }

    public bool TryChooseWanderTarget(
        DateTime nowUtc,
        double currentLeft,
        double currentTop,
        double width,
        double height,
        PetMovementBounds bounds)
    {
        if (!IsWanderDue(nowUtc))
        {
            return false;
        }

        var targetLeft = currentLeft + _random.NextDouble() * WanderRange * 2 - WanderRange;
        var targetTop = currentTop + _random.NextDouble() * WanderRange * 2 - WanderRange;
        SetTarget(PetMovementPlanner.ClampTarget(targetLeft, targetTop, width, height, bounds));
        return true;
    }

    public PetMovementStep? Advance(
        TimeSpan renderingTime,
        double currentLeft,
        double currentTop)
    {
        if (_lastRenderTime is null)
        {
            _lastRenderTime = renderingTime;
            _logicalLeft = currentLeft;
            _logicalTop = currentTop;
            return null;
        }

        var elapsed = renderingTime - _lastRenderTime.Value;
        _lastRenderTime = renderingTime;
        if (!HasTarget || elapsed <= TimeSpan.Zero)
        {
            return null;
        }

        var dx = Target.Left - _logicalLeft;
        var dy = Target.Top - _logicalTop;
        var distanceToTarget = Math.Sqrt(dx * dx + dy * dy);
        var distance = CalculateStep(elapsed, distanceToTarget);
        if (distance <= 0 || distanceToTarget <= 0.001)
        {
            return null;
        }

        var ratio = distance / distanceToTarget;
        var nextLeft = _logicalLeft + dx * ratio;
        var nextTop = _logicalTop + dy * ratio;
        var deltaX = nextLeft - _logicalLeft;
        var deltaY = nextTop - _logicalTop;
        _logicalLeft = nextLeft;
        _logicalTop = nextTop;
        LastDeltaX = deltaX;
        return new PetMovementStep(nextLeft, nextTop, deltaX, deltaY, distance);
    }

    public PetMoveFrameAdvance AdvanceMoveFrame(double distance, int frameCount)
    {
        if (frameCount <= 0 || distance <= 0)
        {
            return new PetMoveFrameAdvance(MoveFrameIndex, Changed: false);
        }

        _frameDistanceAccumulator += distance;
        var distancePerFrame = _settings.DistancePerFrame;
        var changed = false;
        while (_frameDistanceAccumulator >= distancePerFrame)
        {
            _frameDistanceAccumulator -= distancePerFrame;
            MoveFrameIndex = (MoveFrameIndex + 1) % frameCount;
            changed = true;
        }

        return new PetMoveFrameAdvance(MoveFrameIndex, changed);
    }

    public void ResetMoveFrames()
    {
        _frameDistanceAccumulator = 0;
        MoveFrameIndex = 0;
    }

    private double CalculateStep(TimeSpan elapsed, double distanceToTarget)
    {
        if (elapsed <= TimeSpan.Zero || distanceToTarget <= 0)
        {
            return 0;
        }

        var baseSpeed = _settings.BaseSpeedPixelsPerSecond;
        var minSpeed = _settings.MinSpeedPixelsPerSecond;
        var maxSpeed = _settings.MaxSpeedPixelsPerSecond;
        var speed = distanceToTarget > 240 ? maxSpeed
            : distanceToTarget < 80 ? minSpeed
            : baseSpeed;
        return Math.Min(distanceToTarget, speed * elapsed.TotalSeconds);
    }
}
