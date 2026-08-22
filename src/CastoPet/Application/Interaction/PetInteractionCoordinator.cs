namespace CastoPet.Core;

internal sealed class PetInteractionCoordinator
{
    private readonly PetPointerGestureClassifier _pointerGestures;

    public PetInteractionCoordinator(
        WheelCatalog catalog,
        PetPointerGestureClassifier pointerGestures)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _pointerGestures = pointerGestures ?? throw new ArgumentNullException(nameof(pointerGestures));
        RadialWheel = new RadialWheelController(Catalog);
    }

    public WheelCatalog Catalog { get; private set; }
    public RadialWheelController RadialWheel { get; private set; }
    public PetPointerGestureState PointerState => _pointerGestures.State;
    public bool IsRadialWheelOpen { get; private set; }
    public bool HasWheelCategories => Catalog.Categories.Count > 0;

    public PetPointerIntent PressPointer(
        PetPointerButton button,
        double pointerX,
        double pointerY,
        DateTimeOffset now)
    {
        return _pointerGestures.Press(button, pointerX, pointerY, now);
    }

    public PetPointerIntent MovePointer(double pointerX, double pointerY, DateTimeOffset now)
    {
        return _pointerGestures.Move(pointerX, pointerY, now);
    }

    public PetPointerIntent ReleasePointer(
        PetPointerButton button,
        double pointerX,
        double pointerY,
        DateTimeOffset now)
    {
        return _pointerGestures.Release(button, pointerX, pointerY, now);
    }

    public PetPointerIntent UpdateHold(DateTimeOffset now)
    {
        return _pointerGestures.UpdateHold(now);
    }

    public double GetRightHoldProgress(DateTimeOffset now, TimeSpan revealDelay)
    {
        return _pointerGestures.GetRightHoldProgress(now, revealDelay);
    }

    public void CancelPointerGesture()
    {
        _pointerGestures.Cancel();
    }

    public bool TryOpenRadialWheel(DateTimeOffset now)
    {
        if (!HasWheelCategories)
        {
            return false;
        }

        IsRadialWheelOpen = true;
        RadialWheel.Open(now);
        return true;
    }

    public void CloseRadialWheel(bool cancelController)
    {
        CancelPointerGesture();
        if (cancelController && RadialWheel.IsOpen)
        {
            RadialWheel.Cancel();
        }

        IsRadialWheelOpen = false;
    }

    public void UpdateCatalog(WheelCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        CloseRadialWheel(cancelController: true);
        Catalog = catalog;
        RadialWheel = new RadialWheelController(catalog);
    }
}
