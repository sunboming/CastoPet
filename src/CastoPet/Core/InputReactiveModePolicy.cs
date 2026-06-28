namespace CastoPet.Core;

public static class InputReactiveModePolicy
{
    public static bool AllowsPassiveAnimation(bool inputReactiveModeActive)
    {
        return !inputReactiveModeActive;
    }
}
