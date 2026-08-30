namespace CastoPet.Core.Product;

public enum PetShowRestoreAction
{
    ShowAtRuntimePosition,
    RestoreDefaultPosition,
}

public sealed class PetRuntimeState
{
    public bool HasRuntimePosition { get; private set; }
    public double Left { get; private set; }
    public double Top { get; private set; }

    public void SetRuntimePosition(double left, double top)
    {
        Left = left;
        Top = top;
        HasRuntimePosition = true;
    }

    public void ClearRuntimePosition()
    {
        HasRuntimePosition = false;
        Left = 0;
        Top = 0;
    }

    public PetShowRestoreAction GetShowRestoreAction(bool isVisible)
    {
        if (!isVisible)
        {
            return HasRuntimePosition
                ? PetShowRestoreAction.ShowAtRuntimePosition
                : PetShowRestoreAction.RestoreDefaultPosition;
        }

        ClearRuntimePosition();
        return PetShowRestoreAction.RestoreDefaultPosition;
    }
}
