namespace CastoPet.Core.Input;

public static class InputReactiveModePolicy
{
    public static bool AllowsPassiveAnimation(bool inputReactiveModeActive)
    {
        return !inputReactiveModeActive;
    }
}
