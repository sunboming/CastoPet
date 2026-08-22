namespace CastoPet.Core;

public enum PetHorizontalDirection
{
    Left = -1,
    Right = 1,
}

public enum PetFacingDirection
{
    Front,
    Left,
    Right,
}

public enum PetTurnPhase
{
    None,
    ToSide,
    ToFront,
}

public sealed class PetDirectionalMovementAnimator
{
    private PetHorizontalDirection? _desiredDirection;

    public PetFacingDirection Facing { get; private set; } = PetFacingDirection.Front;
    public PetTurnPhase Phase { get; private set; }
    public PetHorizontalDirection TurnDirection { get; private set; }
    public int FrameIndex { get; private set; }
    public bool IsTurning => Phase != PetTurnPhase.None;

    public bool RequestDirection(PetHorizontalDirection direction, int frameCount)
    {
        if (frameCount <= 0)
        {
            return false;
        }

        _desiredDirection = direction;
        if (IsTurning)
        {
            return false;
        }

        if (Facing == ToFacing(direction))
        {
            return false;
        }

        if (Facing == PetFacingDirection.Front)
        {
            StartToSide(direction);
        }
        else
        {
            StartToFront(ToHorizontal(Facing), frameCount);
        }

        return true;
    }

    public bool RequestFront(int frameCount)
    {
        _desiredDirection = null;
        if (frameCount <= 0)
        {
            Reset();
            return false;
        }

        if (Phase == PetTurnPhase.ToSide)
        {
            Phase = PetTurnPhase.ToFront;
            return true;
        }

        if (IsTurning || Facing == PetFacingDirection.Front)
        {
            return IsTurning;
        }

        StartToFront(ToHorizontal(Facing), frameCount);
        return true;
    }

    public void Advance(int frameCount)
    {
        if (!IsTurning || frameCount <= 0)
        {
            return;
        }

        if (Phase == PetTurnPhase.ToSide)
        {
            FrameIndex++;
            if (FrameIndex < frameCount)
            {
                return;
            }

            Facing = ToFacing(TurnDirection);
            StopTurn();
            if (_desiredDirection is { } desired && Facing != ToFacing(desired))
            {
                StartToFront(TurnDirection, frameCount);
            }

            return;
        }

        FrameIndex--;
        if (FrameIndex >= 0)
        {
            return;
        }

        Facing = PetFacingDirection.Front;
        StopTurn();
        if (_desiredDirection is { } pending)
        {
            StartToSide(pending);
        }
    }

    public void Reset()
    {
        _desiredDirection = null;
        Facing = PetFacingDirection.Front;
        StopTurn();
    }

    private void StartToSide(PetHorizontalDirection direction)
    {
        TurnDirection = direction;
        Phase = PetTurnPhase.ToSide;
        FrameIndex = 0;
    }

    private void StartToFront(PetHorizontalDirection direction, int frameCount)
    {
        TurnDirection = direction;
        Phase = PetTurnPhase.ToFront;
        FrameIndex = frameCount - 1;
    }

    private void StopTurn()
    {
        Phase = PetTurnPhase.None;
        FrameIndex = 0;
    }

    private static PetFacingDirection ToFacing(PetHorizontalDirection direction)
    {
        return direction == PetHorizontalDirection.Left
            ? PetFacingDirection.Left
            : PetFacingDirection.Right;
    }

    private static PetHorizontalDirection ToHorizontal(PetFacingDirection facing)
    {
        return facing == PetFacingDirection.Left
            ? PetHorizontalDirection.Left
            : PetHorizontalDirection.Right;
    }
}
