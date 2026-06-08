namespace InferiusQoL.Features.LockerMover;

using System;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using InferiusQoL.UI;
using UnityEngine;

/// <summary>
/// Global MonoBehaviour on a DontDestroyOnLoad host GameObject. In Update(), detects
/// the keybind and the target under the player's cursor. If the cursor points at a
/// supported StorageContainer and the clipboard is in the corresponding state
/// (empty -> grab a full locker; full -> place into an empty locker of the same
/// TechType), performs the action.
///
/// Vanilla selection of what can be moved:
/// - Locker (large locker)
/// - SmallLocker (Wall Locker)
/// - SmallStorage (Waterproof Locker: carryable, but can also be on a wall)
/// - LuggageBag (Carryall)
///
/// Vehicle storage, Nuclear Reactor, Bioreactor, and other special containers are
/// not supported because they have special logic or fixed placement.
/// </summary>
public class LockerMoverManager : MonoBehaviour
{
    private const float DUPLICATE_PRESS_COOLDOWN = 0.3f;
    private float _lastActionTime = -1f;

    private void Update()
    {
        try
        {
            var cfg = InferiusConfig.Instance;
            if (!cfg.LockerMoverEnabled) return;

            var player = Player.main;
            if (player == null) return;

            if (!TryParseKey(cfg.LockerMoverKey, out var key)) return;
            if (!UnityEngine.Input.GetKeyDown(key)) return;
            if (HotkeyFocusGuard.ShouldIgnoreHotkey()) return;

            if (Time.time - _lastActionTime < DUPLICATE_PRESS_COOLDOWN) return;

            if (cfg.LockerMoverRequireEmptyHands && !HasEmptyHands(player))
            {
                ShowToast("Locker Mover: put away the tool in your hand");
                return;
            }

            var target = ResolveHoverTarget(player);
            if (target == null) return;

            _lastActionTime = Time.time;
            HandleAction(target);
        }
        catch (Exception ex)
        {
            QoLLog.Error(Category.LockerMover, "Update threw", ex);
        }
    }

    private static bool HasEmptyHands(Player player)
    {
        var inv = Inventory.main;
        if (inv == null) return true;
        return inv.GetHeldObject() == null;
    }

    private static bool TryParseKey(string name, out KeyCode key)
    {
        if (Enum.TryParse<KeyCode>(name, ignoreCase: true, out key)) return true;
        key = KeyCode.G;
        return false;
    }

    private static HoverTarget? ResolveHoverTarget(Player player)
    {
        var hand = player.guiHand;
        if (hand == null) return null;

        var active = hand.activeTarget;
        if (active == null) return null;

        var sc = active.GetComponentInParent<StorageContainer>();
        if (sc == null) return null;
        if (sc.container == null) return null;

        var techType = ResolveTechType(sc);
        if (!IsSupported(techType)) return null;

        return new HoverTarget
        {
            Container = sc,
            TechType = techType,
        };
    }

    private static bool IsSupported(TechType tt) =>
        tt == TechType.Locker
        || tt == TechType.SmallLocker
        || tt == TechType.SmallStorage
        || tt == TechType.LuggageBag;

    private static TechType ResolveTechType(StorageContainer sc)
    {
        var go = sc.gameObject;

        var techTag = go.GetComponent<TechTag>() ?? go.GetComponentInParent<TechTag>();
        if (techTag != null && techTag.type != TechType.None) return techTag.type;

        var pickupable = go.GetComponent<Pickupable>();
        if (pickupable != null)
        {
            var tt = pickupable.GetTechType();
            if (tt != TechType.None) return tt;
        }

        var constructable = go.GetComponent<Constructable>() ?? go.GetComponentInParent<Constructable>();
        if (constructable != null && constructable.techType != TechType.None) return constructable.techType;

        return TechType.None;
    }

    private void HandleAction(HoverTarget target)
    {
        bool clipboardEmpty = LockerMoverClipboard.IsEmpty;
        bool containerEmpty = target.Container.container.count == 0;

        // Decision table:
        //   clipboard | container | action
        //   empty     | empty     | nothing (silent)
        //   empty     | full      | Grab
        //   full      | empty     | Place (same TechType only)
        //   full      | full      | nothing (toast: place elsewhere or cancel first)

        if (clipboardEmpty && containerEmpty) return;

        if (clipboardEmpty && !containerEmpty)
        {
            DoGrab(target);
            return;
        }

        if (!clipboardEmpty && containerEmpty)
        {
            DoPlace(target);
            return;
        }

        ShowToast($"Locker Mover: clipboard occupied ({LockerMoverClipboard.ItemCount} items, {LockerMoverClipboard.SourceTechType}). Empty it first.");
    }

    private static void DoGrab(HoverTarget target)
    {
        var sc = target.Container;
        int count = LockerMoverClipboard.Grab(sc.container, target.TechType, sc.width, sc.height);
        if (count > 0)
            ShowToast($"Locker Mover: {count} items moved to clipboard");
    }

    private static void DoPlace(HoverTarget target)
    {
        if (LockerMoverClipboard.SourceTechType != target.TechType)
        {
            ShowToast($"Locker Mover: different locker type (clipboard: {LockerMoverClipboard.SourceTechType}, target: {target.TechType})");
            return;
        }

        int before = LockerMoverClipboard.ItemCount;
        bool allPlaced = LockerMoverClipboard.Place(target.Container.container);
        int after = LockerMoverClipboard.ItemCount;
        int placed = before - after;

        if (allPlaced)
            ShowToast($"Locker Mover: {placed} items placed");
        else
            ShowToast($"Locker Mover: placed {placed}/{before}, {after} remaining (not enough room?)");
    }

    private static void ShowToast(string msg)
    {
        try { ErrorMessage.AddMessage(msg); }
        catch { }
        QoLLog.Info(Category.LockerMover, msg);
    }

    private sealed class HoverTarget
    {
        public StorageContainer Container = null!;
        public TechType TechType;
    }
}
