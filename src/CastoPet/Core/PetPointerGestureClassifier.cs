namespace CastoPet.Core;

internal enum PetPointerButton
{
    Left,
    Right,
}

internal enum PetPointerIntent
{
    None,
    Petting,
    Drag,
    ContextMenu,
    RadialWheel,
}

internal enum PetPointerGestureState
{
    Idle,
    LeftPending,
    RightPending,
    Dragging,
    RadialWheel,
}

internal sealed class PetPointerGestureClassifier(
    double leftHorizontalThreshold,
    double leftVerticalThreshold,
    double rightRadialThreshold,
    TimeSpan rightHoldDelay)
{
    private readonly double _leftHorizontalThreshold = Positive(leftHorizontalThreshold, nameof(leftHorizontalThreshold));
    private readonly double _leftVerticalThreshold = Positive(leftVerticalThreshold, nameof(leftVerticalThreshold));
    private readonly double _rightRadialThreshold = Positive(rightRadialThreshold, nameof(rightRadialThreshold));
    private readonly TimeSpan _rightHoldDelay = Positive(rightHoldDelay, nameof(rightHoldDelay));

    public PetPointerGestureState State { get; private set; }
    public double OriginX { get; private set; }
    public double OriginY { get; private set; }
    public DateTimeOffset PressedAt { get; private set; }

    public PetPointerIntent Press(
        PetPointerButton button,
        double pointerX,
        double pointerY,
        DateTimeOffset now)
    {
        if (State != PetPointerGestureState.Idle)
        {
            Cancel();
            return PetPointerIntent.None;
        }

        OriginX = pointerX;
        OriginY = pointerY;
        PressedAt = now;
        State = button == PetPointerButton.Left
            ? PetPointerGestureState.LeftPending
            : PetPointerGestureState.RightPending;
        return PetPointerIntent.None;
    }

    public PetPointerIntent Move(double pointerX, double pointerY, DateTimeOffset now)
    {
        _ = now;
        var deltaX = pointerX - OriginX;
        var deltaY = pointerY - OriginY;
        if (State == PetPointerGestureState.LeftPending
            && (Math.Abs(deltaX) >= _leftHorizontalThreshold
                || Math.Abs(deltaY) >= _leftVerticalThreshold))
        {
            State = PetPointerGestureState.Dragging;
            return PetPointerIntent.Drag;
        }

        if (State == PetPointerGestureState.RightPending
            && Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)) >= _rightRadialThreshold)
        {
            State = PetPointerGestureState.RadialWheel;
            return PetPointerIntent.RadialWheel;
        }

        return PetPointerIntent.None;
    }

    public PetPointerIntent UpdateHold(DateTimeOffset now)
    {
        if (State != PetPointerGestureState.RightPending
            || now - PressedAt < _rightHoldDelay)
        {
            return PetPointerIntent.None;
        }

        State = PetPointerGestureState.RadialWheel;
        return PetPointerIntent.RadialWheel;
    }

    public double GetRightHoldProgress(DateTimeOffset now, TimeSpan revealDelay)
    {
        if (State != PetPointerGestureState.RightPending)
        {
            return 0;
        }

        var elapsed = now - PressedAt;
        if (elapsed < revealDelay)
        {
            return 0;
        }

        var visibleDuration = _rightHoldDelay - revealDelay;
        return visibleDuration <= TimeSpan.Zero
            ? 1
            : Math.Clamp((elapsed - revealDelay).TotalMilliseconds / visibleDuration.TotalMilliseconds, 0, 1);
    }

    public PetPointerIntent Release(
        PetPointerButton button,
        double pointerX,
        double pointerY,
        DateTimeOffset now)
    {
        _ = pointerX;
        _ = pointerY;
        _ = now;
        var expectedState = button == PetPointerButton.Left
            ? PetPointerGestureState.LeftPending
            : PetPointerGestureState.RightPending;
        if (State == expectedState)
        {
            var intent = button == PetPointerButton.Left
                ? PetPointerIntent.Petting
                : PetPointerIntent.ContextMenu;
            Reset();
            return intent;
        }

        Reset();
        return PetPointerIntent.None;
    }

    public void Cancel() => Reset();

    private void Reset()
    {
        State = PetPointerGestureState.Idle;
        OriginX = 0;
        OriginY = 0;
        PressedAt = default;
    }

    private static double Positive(double value, string parameterName) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName);

    private static TimeSpan Positive(TimeSpan value, string parameterName) =>
        value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(parameterName);
}
