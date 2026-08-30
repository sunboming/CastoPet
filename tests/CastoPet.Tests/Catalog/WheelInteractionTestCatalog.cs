namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> WheelInteractionTestCases { get; } =
    [
        new("Expression wheel defines eight items", ExpressionWheelDefinesEightItems),
        new("Expression wheel paths use app resources", ExpressionWheelPathsUseAppResources),
        new("Built-in expression transition actions define shared frames", BuiltInExpressionTransitionActionsDefineSharedFrames),
        new("Expression transition paths use app resources", ExpressionTransitionPathsUseAppResources),
        new("Expression transition planner prefers specific reversible frames", ExpressionTransitionPlannerPrefersSpecificReversibleFrames),
        new("Radial wheel layout keeps generic two-ring geometry", RadialWheelLayoutKeepsGenericTwoRingGeometry),
        new("Radial wheel overlay stays centered on invocation point", RadialWheelOverlayStaysCenteredOnInvocationPoint),
        new("Radial wheel popup overrides WPF edge repositioning", RadialWheelPopupOverridesWpfEdgeRepositioning),
        new("Radial wheel style keeps readable ring hierarchy", RadialWheelStyleKeepsReadableRingHierarchy),
        new("Shortcut wheel loads shell icons", ShortcutWheelLoadsShellIcons),
        new("Pointer gestures classify left click and drag", PointerGesturesClassifyLeftClickAndDrag),
        new("Pointer gestures classify right click movement and hold", PointerGesturesClassifyRightClickMovementAndHold),
        new("Stable pointer gestures keep the traditional context menu", StablePointerGesturesKeepTraditionalContextMenu),
        new("Pointer gestures cancel conflicts and commit once", PointerGesturesCancelConflictsAndCommitOnce),
        new("Interaction coordinator preserves short-click intent", InteractionCoordinatorPreservesShortClickIntent),
        new("Interaction coordinator owns wheel lifecycle", InteractionCoordinatorOwnsWheelLifecycle),
        new("Wheel catalog preserves ordered action references", WheelCatalogPreservesOrderedActionReferences),
        new("Wheel catalog exposes disabled empty shortcut content", WheelCatalogExposesDisabledEmptyShortcutContent),
        new("Wheel catalog service refreshes successful shortcut mutations", WheelCatalogServiceRefreshesSuccessfulShortcutMutations),
        new("Wheel catalog service unsubscribes when disposed", WheelCatalogServiceUnsubscribesWhenDisposed),
        new("Application composes one shared shortcut wheel graph", ApplicationComposesOneSharedShortcutWheelGraph),
        new("Pet window follows live wheel catalog snapshots", PetWindowFollowsLiveWheelCatalogSnapshots),
        new("Radial wheel selector distinguishes all pointer regions", RadialWheelSelectorDistinguishesAllPointerRegions),
        new("Radial wheel second ring stays with category direction", RadialWheelSecondRingStaysWithCategoryDirection),
        new("Radial wheel fast outward motion opens second level", RadialWheelFastOutwardMotionOpensSecondLevel),
        new("Radial wheel controller honors category dwell", RadialWheelControllerHonorsCategoryDwell),
        new("Radial wheel tolerates slight outer overshoot", RadialWheelToleratesSlightOuterOvershoot),
        new("Radial wheel controller resets and collapses state", RadialWheelControllerResetsAndCollapsesState),
        new("Radial wheel controller paginates without persisting controls", RadialWheelControllerPaginatesWithoutPersistingControls),
    ];
}
