namespace CastoPet.Core;

public enum WheelReleaseKind
{
    Cancel,
    Execute,
    PageChanged,
}

public sealed record WheelReleaseResult(WheelReleaseKind Kind, WheelActionItem? Action);

public sealed class RadialWheelController
{
    private static readonly WheelActionItem PreviousPageItem =
        new("wheel-previous-page", "上一页", WheelActionType.PreviousPage, null);
    private static readonly WheelActionItem NextPageItem =
        new("wheel-next-page", "下一页", WheelActionType.NextPage, null);

    private readonly WheelCatalog catalog;
    private int dwellCategoryIndex = -1;
    private DateTimeOffset dwellStartedAt;

    public RadialWheelController(WheelCatalog catalog)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public bool IsOpen { get; private set; }
    public bool IsSecondLevelOpen { get; private set; }
    public int SelectedCategoryIndex { get; private set; } = -1;
    public int SelectedSecondLevelIndex { get; private set; } = -1;
    public int CurrentPage { get; private set; }
    public IReadOnlyList<WheelActionItem> VisibleSecondLevelItems { get; private set; } = [];

    public void Open(DateTimeOffset now)
    {
        IsOpen = true;
        CollapseSecondLevel();
        dwellCategoryIndex = -1;
        dwellStartedAt = now;
    }

    public void UpdatePointer(double pointerX, double pointerY, DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return;
        }

        var selection = RadialWheelSelector.GetSelection(
            pointerX,
            pointerY,
            catalog.Categories.Count,
            VisibleSecondLevelItems.Count,
            SelectedCategoryIndex);

        switch (selection.Ring)
        {
            case RadialWheelRing.Center:
                CollapseSecondLevel();
                dwellCategoryIndex = -1;
                break;
            case RadialWheelRing.First:
                UpdateCategoryDwell(selection.SectorIndex, now);
                break;
            case RadialWheelRing.Second:
                if (!IsSecondLevelOpen)
                {
                    UpdateCategoryDwell(
                        RadialWheelSelector.GetCategoryIndex(pointerX, pointerY, catalog.Categories.Count),
                        now);
                    if (IsSecondLevelOpen)
                    {
                        selection = RadialWheelSelector.GetSelection(
                            pointerX,
                            pointerY,
                            catalog.Categories.Count,
                            VisibleSecondLevelItems.Count,
                            SelectedCategoryIndex);
                    }
                }

                SelectedSecondLevelIndex = IsSecondLevelOpen ? selection.SectorIndex : -1;
                break;
            case RadialWheelRing.Outside:
                Cancel();
                break;
        }
    }

    public WheelReleaseResult ReleaseSecondLevelItem(int itemIndex)
    {
        if (!IsOpen || !IsSecondLevelOpen || itemIndex < 0 || itemIndex >= VisibleSecondLevelItems.Count)
        {
            return Cancel();
        }

        var item = VisibleSecondLevelItems[itemIndex];
        if (item.ActionType == WheelActionType.PreviousPage)
        {
            CurrentPage--;
            RefreshVisibleItems();
            return new WheelReleaseResult(WheelReleaseKind.PageChanged, null);
        }

        if (item.ActionType == WheelActionType.NextPage)
        {
            CurrentPage++;
            RefreshVisibleItems();
            return new WheelReleaseResult(WheelReleaseKind.PageChanged, null);
        }

        if (!item.IsEnabled || item.ActionType == WheelActionType.Disabled)
        {
            return Cancel();
        }

        Close();
        return new WheelReleaseResult(WheelReleaseKind.Execute, item);
    }

    public WheelReleaseResult Release()
    {
        return ReleaseSecondLevelItem(SelectedSecondLevelIndex);
    }

    public WheelReleaseResult Cancel()
    {
        Close();
        return new WheelReleaseResult(WheelReleaseKind.Cancel, null);
    }

    private void UpdateCategoryDwell(int categoryIndex, DateTimeOffset now)
    {
        if (categoryIndex < 0 || categoryIndex >= catalog.Categories.Count)
        {
            return;
        }

        if (categoryIndex != dwellCategoryIndex)
        {
            dwellCategoryIndex = categoryIndex;
            dwellStartedAt = now;
            CollapseSecondLevel();
            SelectedCategoryIndex = categoryIndex;
            return;
        }

        SelectedCategoryIndex = categoryIndex;
        if (!IsSecondLevelOpen && now - dwellStartedAt >= WheelCatalog.CategoryDwellDelay)
        {
            IsSecondLevelOpen = true;
            CurrentPage = 0;
            RefreshVisibleItems();
        }
    }

    private void RefreshVisibleItems()
    {
        var actions = catalog.Categories[SelectedCategoryIndex].Items;
        var pages = BuildPages(actions);
        CurrentPage = Math.Clamp(CurrentPage, 0, pages.Count - 1);
        var visible = new List<WheelActionItem>(WheelCatalog.MaxVisibleItemsPerRing);
        if (CurrentPage > 0)
        {
            visible.Add(PreviousPageItem);
        }

        visible.AddRange(pages[CurrentPage]);
        if (CurrentPage < pages.Count - 1)
        {
            visible.Add(NextPageItem);
        }

        VisibleSecondLevelItems = visible;
        SelectedSecondLevelIndex = -1;
    }

    private static IReadOnlyList<IReadOnlyList<WheelActionItem>> BuildPages(IReadOnlyList<WheelActionItem> actions)
    {
        if (actions.Count <= WheelCatalog.MaxVisibleItemsPerRing)
        {
            return [actions];
        }

        var pages = new List<IReadOnlyList<WheelActionItem>>();
        var offset = 0;
        while (offset < actions.Count)
        {
            var hasPrevious = pages.Count > 0;
            var available = WheelCatalog.MaxVisibleItemsPerRing - (hasPrevious ? 1 : 0);
            var remaining = actions.Count - offset;
            var take = remaining <= available ? remaining : available - 1;
            pages.Add(actions.Skip(offset).Take(take).ToArray());
            offset += take;
        }

        return pages;
    }

    private void CollapseSecondLevel()
    {
        IsSecondLevelOpen = false;
        SelectedCategoryIndex = -1;
        SelectedSecondLevelIndex = -1;
        CurrentPage = 0;
        VisibleSecondLevelItems = [];
    }

    private void Close()
    {
        IsOpen = false;
        CollapseSecondLevel();
        dwellCategoryIndex = -1;
    }
}
