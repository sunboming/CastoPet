namespace CastoPet.Core.Movement;

public sealed class CursorPushGate
{
    public bool AllowsPush { get; private set; } = true;

    public void CompletePush()
    {
        AllowsPush = false;
    }

    public void ObserveCursorDistance(double cursorDistance, double interestRadius)
    {
        if (cursorDistance > interestRadius)
        {
            AllowsPush = true;
        }
    }

    public void Reset()
    {
        AllowsPush = true;
    }
}
