namespace InferiusQoL.Features.LockerMover;

using System.Collections.Generic;
using System.Linq;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// In-memory clipboard. Pouziva skryty <see cref="ItemsContainer"/> jako
/// uloziste - diky tomu se Pickupable radne deaktivuji (physics, collidery,
/// rendering) pres vanilla logiku AddItem/RemoveItem. Primy transform.SetParent
/// + SetActive(false) nestacil - Subnautica pickupable po RemoveItem vracela
/// do fyziky a vznikala duplicita na zemi.
///
/// Poradi: InventoryItem seznam drzime v originalnim iteration order pro
/// pripadne budouci re-place. ItemsContainer.AddItem vola first-fit top-left
/// algoritmus - re-place ve stejnem poradi reprodukuje podobne rozlozeni.
/// </summary>
public static class LockerMoverClipboard
{
    private const int CLIPBOARD_SIZE = 16; // dost velky at se vejde vsechno z Lockeru 6x8 i WallLocker 4x5

    private static GameObject? _hostGO;
    private static ItemsContainer? _buffer;
    private static readonly List<Pickupable> _order = new List<Pickupable>();

    public static TechType SourceTechType { get; private set; } = TechType.None;
    public static int SourceWidth { get; private set; } = 0;
    public static int SourceHeight { get; private set; } = 0;

    public static bool HasContent => _order.Count > 0;
    public static int ItemCount => _order.Count;
    public static bool IsEmpty => _order.Count == 0;

    public static bool IsClipboardContainer(ItemsContainer? container) =>
        _buffer != null && ReferenceEquals(container, _buffer);

    /// <summary>
    /// Presune obsah container do skryteho bufferu.
    /// </summary>
    public static int Grab(ItemsContainer source, TechType sourceTT, int width, int height)
    {
        if (source == null) return 0;
        if (!IsEmpty)
        {
            QoLLog.Warning(Category.LockerMover, "Grab called while clipboard not empty - aborting");
            return 0;
        }

        EnsureBuffer();

        // Nejprve sesbirame snapshot InventoryItems. Modifikujeme behem iterace,
        // takze musi byt kopie.
        var snapshot = source.ToList();

        int taken = 0;
        foreach (var invItem in snapshot)
        {
            var pickupable = invItem?.item;
            if (pickupable == null) continue;

            // Transfer: odebereme ze zdroje, pridame do bufferu. ItemsContainer.AddItem
            // handluje reparent + physics disable - stejny kod jako vanilla presun
            // itemu mezi lockery.
            if (!source.RemoveItem(pickupable, true))
            {
                QoLLog.Warning(Category.LockerMover, $"RemoveItem failed for {pickupable.GetTechType()}");
                continue;
            }

            var added = _buffer!.AddItem(pickupable);
            if (added == null)
            {
                // Buffer plny nebo item zamitnut - vratime do zdroje aby neskoncil ztraceny.
                QoLLog.Warning(Category.LockerMover,
                    $"Buffer rejected {pickupable.GetTechType()} - returning to source");
                source.AddItem(pickupable);
                continue;
            }

            _order.Add(pickupable);
            taken++;
        }

        SourceTechType = sourceTT;
        SourceWidth = width;
        SourceHeight = height;
        QoLLog.Info(Category.LockerMover,
            $"Grabbed {taken} items from {sourceTT} ({width}x{height})");
        return taken;
    }

    /// <summary>
    /// Presune obsah bufferu do cilove container. Poradi dle _order listu
    /// (= iteration order ze zdroje). AddItem do cile dela first-fit layout.
    /// </summary>
    public static bool Place(ItemsContainer target)
    {
        if (target == null) return false;
        if (IsEmpty) return false;

        int placed = 0;
        var remaining = new List<Pickupable>();

        // Kopie protoze buffer.RemoveItem+target.AddItem menime behem iterace.
        var orderSnapshot = new List<Pickupable>(_order);
        foreach (var p in orderSnapshot)
        {
            if (p == null) continue;

            if (!_buffer!.RemoveItem(p, true))
            {
                QoLLog.Warning(Category.LockerMover,
                    $"Buffer RemoveItem failed for {p.GetTechType()} during place");
                remaining.Add(p);
                continue;
            }

            var added = target.AddItem(p);
            if (added != null)
            {
                placed++;
            }
            else
            {
                // Cil plny - vratime do bufferu.
                _buffer!.AddItem(p);
                remaining.Add(p);
            }
        }

        _order.Clear();
        _order.AddRange(remaining);

        QoLLog.Info(Category.LockerMover,
            $"Placed {placed} items; {remaining.Count} left in clipboard");

        if (_order.Count == 0)
        {
            SourceTechType = TechType.None;
            SourceWidth = 0;
            SourceHeight = 0;
            return true;
        }

        return false;
    }

    public static void Clear()
    {
        if (_buffer != null)
        {
            var all = _buffer.ToList();
            foreach (var invItem in all)
            {
                if (invItem?.item != null)
                {
                    _buffer.RemoveItem(invItem.item, true);
                    Object.Destroy(invItem.item.gameObject);
                }
            }
        }
        _order.Clear();
        SourceTechType = TechType.None;
        SourceWidth = 0;
        SourceHeight = 0;
    }

    private static void EnsureBuffer()
    {
        if (_buffer != null && _hostGO != null) return;

        _hostGO = new GameObject("InferiusQoL_LockerMoverClipboardHost");
        Object.DontDestroyOnLoad(_hostGO);
        // Host GO je aktivni (aby ItemsContainer fungoval), ale nema renderer,
        // takze neni videt. Pickupable parenti se do nej a jsou tim schovany
        // - ItemsContainer sam je prepne do "stored" modu (physics off, hidden).

        _buffer = new ItemsContainer(
            CLIPBOARD_SIZE,
            CLIPBOARD_SIZE,
            _hostGO.transform,
            "InferiusQoL_LockerMoverClipboard",
            null);
    }
}
