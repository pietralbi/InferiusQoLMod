#nullable enable
namespace InferiusQoL.Features.InventoryResize;

using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Features.Backpacks;
using InferiusQoL.Logging;

[HarmonyPatch(typeof(Inventory), nameof(Inventory.Awake))]
public static class InventoryResizePatch
{
    // Vanilla values. Keep them so a runtime toggle can return the inventory to
    // vanilla size instead of enlarging it further.
    private static int _vanillaWidth = -1;
    private static int _vanillaHeight = -1;

    [HarmonyPostfix]
    public static void Postfix(Inventory __instance)
    {
        if (__instance?.container == null) return;

        if (_vanillaWidth < 0)
        {
            _vanillaWidth = __instance.container.sizeX;
            _vanillaHeight = __instance.container.sizeY;
            QoLLog.Debug(Category.Inventory,
                $"Vanilla inventory size recorded: {_vanillaWidth}x{_vanillaHeight}");
        }

        // Hook equipment changes so equipping/unequipping a backpack triggers an
        // inventory resize without needing a restart or config change.
        HookEquipmentEvents(__instance);

        ApplyTo(__instance, InferiusConfig.Instance);
    }

    private static Equipment? _hookedEquipment;

    private static void HookEquipmentEvents(Inventory inv)
    {
        if (inv?.equipment == null) return;
        if (ReferenceEquals(_hookedEquipment, inv.equipment)) return;

        if (_hookedEquipment != null)
        {
            _hookedEquipment.onEquip -= OnEquipmentChanged;
            _hookedEquipment.onUnequip -= OnEquipmentChanged;
        }

        _hookedEquipment = inv.equipment;
        _hookedEquipment.onEquip += OnEquipmentChanged;
        _hookedEquipment.onUnequip += OnEquipmentChanged;
        QoLLog.Debug(Category.Inventory, "Equipment events hooked for current Inventory equipment (onEquip/onUnequip)");
    }

    private static void OnEquipmentChanged(string slot, InventoryItem item)
    {
        // React only to our backpacks; other equipment changes do not matter here.
        if (item?.item == null) return;
        var tt = item.item.GetTechType();
        if (tt != Backpacks.BackpackItems.Small
            && tt != Backpacks.BackpackItems.Medium
            && tt != Backpacks.BackpackItems.Large) return;

        QoLLog.Debug(Category.Inventory, $"Backpack equipment change ({slot}, {tt}) - reapplying inventory");
        ApplyRuntime();
    }

    public static void ApplyRuntime(InferiusConfig? cfg = null)
    {
        if (Inventory.main == null)
        {
            QoLLog.Debug(Category.Inventory, "ApplyRuntime: Inventory.main is null, skipping");
            return;
        }
        try
        {
            ApplyTo(Inventory.main, cfg ?? InferiusConfig.Instance);
        }
        catch (System.Exception ex)
        {
            QoLLog.Error(Category.Inventory, "ApplyRuntime failed (external mod patch may have thrown)", ex);
        }
    }

    private static void ApplyTo(Inventory inv, InferiusConfig cfg)
    {
        if (inv?.container == null) return;
        if (_vanillaWidth < 0) return;

        int targetW, targetH;

        if (cfg.InventoryResizeEnabled)
        {
            targetW = _vanillaWidth + cfg.InventoryExtraCols;
            targetH = _vanillaHeight + cfg.InventoryExtraRows;
        }
        else
        {
            targetW = _vanillaWidth;
            targetH = _vanillaHeight;
        }

        // Bonus from a backpack equipped in Player equipment (Chip slot).
        if (cfg.BackpacksEnabled && !Plugin.HasBagEquipment)
        {
            var tier = BackpackItems.GetEquippedTier();
            if (tier != BackpackTier.None)
            {
                targetH += BackpackItems.GetTierRows(tier, cfg);
            }
        }

        var curW = inv.container.sizeX;
        var curH = inv.container.sizeY;
        if (curW == targetW && curH == targetH)
        {
            QoLLog.Trace(Category.Inventory, $"ApplyTo: already {targetW}x{targetH}, skip");
            return;
        }

        // Just call Resize. Subnautica's vanilla ItemsContainer.Resize checks
        // whether items fit; if not, it is a no-op with no data loss. Our old
        // "safety" check counted item quantity (for example 12 titanium = 12)
        // instead of slots (12 titanium = 1 slot stack), which blocked legitimate
        // shrink operations.
        inv.container.Resize(targetW, targetH);

        // Verify whether resize succeeded; Subnautica may reject it if items do not fit.
        var afterW = inv.container.sizeX;
        var afterH = inv.container.sizeY;
        if (afterW == targetW && afterH == targetH)
        {
            QoLLog.Info(Category.Inventory, $"Inventory resized: {curW}x{curH} -> {afterW}x{afterH}");
        }
        else
        {
            QoLLog.Warning(Category.Inventory,
                $"Inventory resize {curW}x{curH} -> {targetW}x{targetH} rejected by vanilla; stayed at {afterW}x{afterH}");
        }
    }
}
