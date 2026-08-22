namespace CastoPet.Core;

public enum InputReactiveEventKind
{
    KeyDown,
    MouseDown,
}

public readonly record struct InputReactiveEvent(InputReactiveEventKind Kind, string Id);
