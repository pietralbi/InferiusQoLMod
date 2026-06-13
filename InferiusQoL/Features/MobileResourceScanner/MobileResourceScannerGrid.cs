namespace InferiusQoL.Features.MobileResourceScanner;

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class MobileResourceScannerGrid : MonoBehaviour, uGUI_INavigableIconGrid
{
    private readonly List<MobileResourceScannerTile> _tiles = new List<MobileResourceScannerTile>();
    private readonly List<MobileResourceScannerTile> _visibleTiles = new List<MobileResourceScannerTile>();
    private int _selectedIndex = -1;
    private bool _settingSelection;

    internal int Columns { get; set; } = 4;

    public bool ShowSelector => true;

    public bool EmulateRaycast => false;

    internal void Register(MobileResourceScannerTile tile)
    {
        if (tile == null || _tiles.Contains(tile))
            return;

        tile.Grid = this;
        _tiles.Add(tile);
    }

    internal void RefreshVisibleItems()
    {
        var selectedTile = GetSelectedTile();
        _visibleTiles.Clear();

        foreach (var tile in _tiles)
        {
            if (tile != null && tile.gameObject.activeInHierarchy)
                _visibleTiles.Add(tile);
        }

        var index = selectedTile == null ? -1 : _visibleTiles.IndexOf(selectedTile);
        if (selectedTile != null && index >= 0)
        {
            _selectedIndex = index;
            SelectTile(selectedTile);
        }
        else
        {
            DeselectItem();
            SelectFirstItem();
        }
    }

    internal void SelectTile(MobileResourceScannerTile tile)
    {
        if (tile == null)
            return;

        var index = _visibleTiles.IndexOf(tile);
        if (index < 0)
            return;

        var previous = GetSelectedTile();
        if (previous != null && previous != tile)
            previous.SetFocused(false);

        _selectedIndex = index;
        tile.SetFocused(true);

        if (_settingSelection || EventSystem.current == null || EventSystem.current.currentSelectedGameObject == tile.gameObject)
            return;

        _settingSelection = true;
        tile.Button.Select();
        _settingSelection = false;
    }

    public object GetSelectedItem()
    {
        return GetSelectedTile()?.TechType ?? TechType.None;
    }

    public Graphic GetSelectedIcon()
    {
        return GetSelectedTile()?.SelectionGraphic
            ?? (_visibleTiles.Count > 0 ? _visibleTiles[0].SelectionGraphic : null!);
    }

    public void SelectItem(object item)
    {
        if (item is MobileResourceScannerTile tile)
        {
            SelectTile(tile);
            return;
        }

        if (item is TechType techType)
        {
            foreach (var candidate in _visibleTiles)
            {
                if (candidate.TechType == techType)
                {
                    SelectTile(candidate);
                    return;
                }
            }
        }

        DeselectItem();
    }

    public void DeselectItem()
    {
        var tile = GetSelectedTile();
        if (tile != null)
            tile.SetFocused(false);

        _selectedIndex = -1;
    }

    public bool SelectFirstItem()
    {
        if (_visibleTiles.Count == 0)
            return false;

        SelectTile(_visibleTiles[0]);
        return true;
    }

    public bool SelectItemClosestToPosition(Vector3 worldPos)
    {
        if (_visibleTiles.Count == 0)
            return false;

        MobileResourceScannerTile? closest = null;
        var closestDistance = float.MaxValue;
        foreach (var tile in _visibleTiles)
        {
            var distance = (tile.transform.position - worldPos).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closest = tile;
            closestDistance = distance;
        }

        if (closest == null)
            return false;

        SelectTile(closest);
        return true;
    }

    public bool SelectItemInDirection(int dirX, int dirY)
    {
        if (_visibleTiles.Count == 0)
            return false;

        if (_selectedIndex < 0)
            return SelectFirstItem();

        var next = _selectedIndex;
        if (dirX != 0)
            next += dirX > 0 ? 1 : -1;
        else if (dirY != 0)
            next += dirY > 0 ? -Columns : Columns;

        next = Mathf.Clamp(next, 0, _visibleTiles.Count - 1);
        if (next == _selectedIndex)
            return false;

        SelectTile(_visibleTiles[next]);
        return true;
    }

    public uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY)
    {
        return this;
    }

    private MobileResourceScannerTile? GetSelectedTile()
    {
        return _selectedIndex >= 0 && _selectedIndex < _visibleTiles.Count ? _visibleTiles[_selectedIndex] : null;
    }
}

internal sealed class MobileResourceScannerTile : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    internal static readonly Color NormalColor = new Color(0.035f, 0.15f, 0.18f, 0.88f);
    private static readonly Color CurrentColor = new Color(0.12f, 0.24f, 0.24f, 0.95f);
    private static readonly Color FocusedColor = new Color(0.08f, 0.34f, 0.39f, 0.98f);
    private static readonly Color CurrentFocusedColor = new Color(0.25f, 0.34f, 0.25f, 0.98f);

    private Image _background = null!;
    private Image _accent = null!;
    private TextMeshProUGUI _label = null!;
    private string _filterText = string.Empty;

    internal TechType TechType { get; private set; }

    internal Button Button { get; private set; } = null!;

    internal Graphic SelectionGraphic => _background;

    internal MobileResourceScannerGrid Grid { get; set; } = null!;

    private bool IsCurrent { get; set; }

    internal void Initialize(TechType techType, Button button, Image background, Image accent, uGUI_Icon icon, TextMeshProUGUI label)
    {
        TechType = techType;
        Button = button;
        _background = background;
        _accent = accent;
        _label = label;
        _filterText = label.text.ToLowerInvariant();
        SetFocused(false);
    }

    internal bool MatchesFilter(string lower)
    {
        return _filterText.Contains(lower);
    }

    internal void SetCurrent(bool current)
    {
        IsCurrent = current;
        _accent.gameObject.SetActive(current);
        SetFocused(false);
    }

    internal void SetFocused(bool focused)
    {
        _background.color = focused
            ? (IsCurrent ? CurrentFocusedColor : FocusedColor)
            : (IsCurrent ? CurrentColor : NormalColor);

        _label.color = focused ? Color.white : new Color(0.92f, 1f, 1f, 0.94f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Grid?.SelectTile(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Grid?.SelectTile(this);
    }
}
