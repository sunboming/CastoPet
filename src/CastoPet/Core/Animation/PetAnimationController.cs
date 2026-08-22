namespace CastoPet.Core.Animation;

public enum PetExpressionTransitionMode
{
    None,
    In,
    Out,
}

public readonly record struct PetFrameAdvance(int FrameIndex, bool Completed);

public readonly record struct PetExpressionTransitionAdvance(
    int FrameIndex,
    bool Completed,
    PetExpressionTransitionMode CompletedMode);

public readonly record struct PetPassiveAnimationContext(
    bool PassiveAnimationAllowed,
    bool IsDragging,
    bool HasActiveMovementTarget,
    bool IsRadialWheelOpen,
    bool HasTemporaryExpression);

public sealed class PetAnimationController
{
    public int IdleFrameIndex { get; private set; }
    public int BlinkFrameIndex { get; private set; }
    public int PettingFrameIndex { get; private set; }
    public int ExpressionTransitionFrameIndex { get; private set; }
    public bool IsBlinking { get; private set; }
    public bool IsPetting { get; private set; }
    public PetExpressionTransitionMode ExpressionTransitionMode { get; private set; }

    public int AdvanceIdle(int frameCount)
    {
        IdleFrameIndex = frameCount <= 0 ? 0 : (IdleFrameIndex + 1) % frameCount;
        return IdleFrameIndex;
    }

    public void ResetIdle()
    {
        IdleFrameIndex = 0;
    }

    public bool BeginBlink(int frameCount)
    {
        if (frameCount <= 0 || IsBlinking)
        {
            return false;
        }

        IsBlinking = true;
        BlinkFrameIndex = 0;
        return true;
    }

    public PetFrameAdvance AdvanceBlink(int frameCount)
    {
        if (!IsBlinking || frameCount <= 0)
        {
            StopBlink();
            return new PetFrameAdvance(0, Completed: true);
        }

        BlinkFrameIndex++;
        if (BlinkFrameIndex < frameCount)
        {
            return new PetFrameAdvance(BlinkFrameIndex, Completed: false);
        }

        StopBlink();
        return new PetFrameAdvance(0, Completed: true);
    }

    public void StopBlink()
    {
        IsBlinking = false;
        BlinkFrameIndex = 0;
    }

    public bool BeginPetting(int frameCount)
    {
        if (frameCount <= 0)
        {
            return false;
        }

        IsPetting = true;
        PettingFrameIndex = 0;
        return true;
    }

    public PetFrameAdvance AdvancePetting(int frameCount)
    {
        if (!IsPetting || frameCount <= 0)
        {
            StopPetting();
            return new PetFrameAdvance(0, Completed: true);
        }

        PettingFrameIndex++;
        if (PettingFrameIndex < frameCount)
        {
            return new PetFrameAdvance(PettingFrameIndex, Completed: false);
        }

        StopPetting();
        return new PetFrameAdvance(0, Completed: true);
    }

    public void StopPetting()
    {
        IsPetting = false;
        PettingFrameIndex = 0;
    }

    public bool BeginExpressionTransition(PetExpressionTransitionMode mode, int frameCount)
    {
        if (mode == PetExpressionTransitionMode.None || frameCount <= 0)
        {
            StopExpressionTransition();
            return false;
        }

        ExpressionTransitionMode = mode;
        ExpressionTransitionFrameIndex = 0;
        return true;
    }

    public PetExpressionTransitionAdvance AdvanceExpressionTransition(int frameCount)
    {
        var mode = ExpressionTransitionMode;
        if (mode == PetExpressionTransitionMode.None || frameCount <= 0)
        {
            StopExpressionTransition();
            return new PetExpressionTransitionAdvance(0, Completed: true, mode);
        }

        ExpressionTransitionFrameIndex++;
        if (ExpressionTransitionFrameIndex < frameCount)
        {
            return new PetExpressionTransitionAdvance(
                ExpressionTransitionFrameIndex,
                Completed: false,
                PetExpressionTransitionMode.None);
        }

        StopExpressionTransition();
        return new PetExpressionTransitionAdvance(0, Completed: true, mode);
    }

    public void StopExpressionTransition()
    {
        ExpressionTransitionMode = PetExpressionTransitionMode.None;
        ExpressionTransitionFrameIndex = 0;
    }

    public bool CanRunIdle(PetPassiveAnimationContext context, int frameCount)
    {
        return context.PassiveAnimationAllowed
            && !context.IsDragging
            && !IsPetting
            && !context.HasActiveMovementTarget
            && !context.IsRadialWheelOpen
            && !context.HasTemporaryExpression
            && ExpressionTransitionMode == PetExpressionTransitionMode.None
            && frameCount > 0;
    }

    public bool CanBeginBlink(PetPassiveAnimationContext context, int frameCount)
    {
        return CanRunIdle(context, frameCount)
            && !IsBlinking;
    }
}
