namespace InferiusQoL.Features.LockerResize;

using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;

[HarmonyPatch(typeof(StorageContainer), nameof(StorageContainer.Awake))]
public static class StorageContainer_Awake_Patch
{
    [HarmonyPostfix]
    public static void Postfix(StorageContainer __instance)
    {
        var cfg = InferiusConfig.Instance;

        if (!cfg.LockerResizeEnabled) return;
        if (Plugin.HasCustomizedStorage) return;

        var techType = ResolveTechType(__instance);

        if (!TryGetTargetSize(techType, __instance, cfg, out int newW, out int newH))
        {
            QoLLog.Trace(Category.Locker,
                $"Unknown storage: go='{__instance.gameObject.name}' tt={techType} size={__instance.width}x{__instance.height}");
            return;
        }

        if (newW <= 0 || newH <= 0) return;
        if (__instance.width == newW && __instance.height == newH)
        {
            QoLLog.Trace(Category.Locker, $"{techType}: already {newW}x{newH}, skip");
            return;
        }

        var oldW = __instance.width;
        var oldH = __instance.height;

        __instance.Resize(newW, newH);
        QoLLog.Info(Category.Locker,
            $"{techType} ('{__instance.gameObject.name}'): {oldW}x{oldH} -> {newW}x{newH}");
    }

    private static bool TryGetTargetSize(TechType techType, StorageContainer sc, InferiusConfig cfg, out int w, out int h)
    {
        switch (techType)
        {
            case TechType.Locker:
                w = cfg.LockerWidth; h = cfg.LockerHeight; return true;
            case TechType.SmallLocker:
                w = cfg.WallLockerWidth; h = cfg.WallLockerHeight; return true;
            case TechType.SmallStorage:
                w = cfg.WaterproofLockerWidth; h = cfg.WaterproofLockerHeight; return true;
            case TechType.LuggageBag:
                w = cfg.CarryallWidth; h = cfg.CarryallHeight; return true;
            case TechType.VehicleStorageModule:
                w = cfg.VehicleStorageWidth; h = cfg.VehicleStorageHeight; return true;
        }

        // Vehicle storage slots do not have TechType on the StorageContainer GameObject.
        // Detect them through the SeamothStorageContainer component or parent Vehicle.
        if (IsVehicleStorageSlot(sc))
        {
            w = cfg.VehicleStorageWidth; h = cfg.VehicleStorageHeight; return true;
        }

        w = 0; h = 0; return false;
    }

    private static bool IsVehicleStorageSlot(StorageContainer sc)
    {
        // Seamoth storage slot: SeamothStorageContainer component wraps StorageContainer
        if (sc.GetComponent<SeamothStorageContainer>() != null) return true;
        if (sc.GetComponentInParent<SeamothStorageContainer>() != null) return true;

        // Prawn/Exosuit storage slot: StorageContainer is a direct deep child of the
        // Exosuit prefab. Verify that the parent is a Vehicle (Exosuit/Seamoth) and
        // that this is not the main upgrade slot.
        var vehicle = sc.GetComponentInParent<Vehicle>();
        if (vehicle != null)
        {
            // Exclude battery and upgrade slots, which also have a Vehicle parent but
            // are not StorageContainer. This is already filtered by type, but keep it
            // for safety. GameObject name typically contains "Storage" for storage.
            var name = sc.gameObject.name;
            if (name.IndexOf("storage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static TechType ResolveTechType(StorageContainer sc)
    {
        var go = sc.gameObject;

        var techTag = go.GetComponent<TechTag>();
        if (techTag != null && techTag.type != TechType.None)
            return techTag.type;

        var pickupable = go.GetComponent<Pickupable>();
        if (pickupable != null)
        {
            var tt = pickupable.GetTechType();
            if (tt != TechType.None) return tt;
        }

        var constructable = go.GetComponent<Constructable>();
        if (constructable != null && constructable.techType != TechType.None)
            return constructable.techType;

        var parentTechTag = go.GetComponentInParent<TechTag>();
        if (parentTechTag != null && parentTechTag.type != TechType.None)
            return parentTechTag.type;

        var parentConstructable = go.GetComponentInParent<Constructable>();
        if (parentConstructable != null && parentConstructable.techType != TechType.None)
            return parentConstructable.techType;

        return TechType.None;
    }

    /// <summary>
    /// Iterates all StorageContainers in the scene and applies the current config size.
    /// Called when config changes in Options (ConfigSavePatch).
    /// </summary>
    public static void ApplyRuntime()
    {
        var cfg = InferiusConfig.Instance;
        if (!cfg.LockerResizeEnabled) return;
        if (Plugin.HasCustomizedStorage) return;

        var containers = UnityEngine.Object.FindObjectsOfType<StorageContainer>();
        int resized = 0;
        foreach (var sc in containers)
        {
            if (sc == null) continue;
            var techType = ResolveTechType(sc);
            if (!TryGetTargetSize(techType, sc, cfg, out int newW, out int newH)) continue;
            if (newW <= 0 || newH <= 0) continue;
            if (sc.width == newW && sc.height == newH) continue;

            sc.Resize(newW, newH);
            resized++;
        }

        if (resized > 0)
            QoLLog.Info(Category.Locker, $"ApplyRuntime: {resized} lockers resized");
    }
}
