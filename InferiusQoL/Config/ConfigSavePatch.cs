namespace InferiusQoL.Config;

using System.Reflection;
using HarmonyLib;
using InferiusQoL.Features.BaseGlassHull;
using InferiusQoL.Features.Batteries;
using InferiusQoL.Features.CoralShellRegen;
using InferiusQoL.Features.InventoryStacking;
using InferiusQoL.Features.InventoryResize;
using InferiusQoL.Features.LockerResize;
using InferiusQoL.Features.OxygenRefill;
using InferiusQoL.Features.ScrollableInventory;
using InferiusQoL.Logging;
using Nautilus.Json;

/// <summary>
/// Postfix for ConfigFile.Save(). Called whenever Nautilus saves the config after
/// a slider or toggle changes in the Options menu, thanks to SaveOn = ChangeValue.
/// Ensures that features capable of absorbing runtime changes apply them after
/// the config changes.
///
/// IMPORTANT: during save, Nautilus may use a config instance that is NOT our
/// singleton and has DEFAULT values for fields the user did not change in this
/// save. If we passed __instance to ApplyRuntime, defaults would overwrite
/// user-set values from the singleton. For example, moving a range slider would
/// reset the inventory to vanilla size. So we merge changed fields from
/// __instance into the singleton, then ApplyRuntime is called with the updated
/// singleton.
/// </summary>
[HarmonyPatch(typeof(ConfigFile), nameof(ConfigFile.Save))]
public static class ConfigSavePatch
{
    [HarmonyPostfix]
    public static void Postfix(ConfigFile __instance)
    {
        if (!(__instance is InferiusConfig cfg)) return;

        var singleton = InferiusConfig.Instance;
        var isSingleton = ReferenceEquals(cfg, singleton);

        // Non-singleton: reload the singleton from the JSON that Nautilus just
        // wrote so it has ALL current user values plus the newly changed field.
        if (!isSingleton)
        {
            try
            {
                singleton.Load();
                QoLLog.Debug(Category.Config,
                    $"Non-singleton Save -> singleton reloaded from JSON. rows={singleton.InventoryExtraRows}");
            }
            catch (System.Exception ex)
            {
                QoLLog.Error(Category.Config, "Singleton reload failed", ex);
            }
        }

        QoLLog.Info(Category.Config,
            $"Save: singleton rows={singleton.InventoryExtraRows} cols={singleton.InventoryExtraCols} "
            + $"(__instance rows={cfg.InventoryExtraRows}, same={isSingleton})");

        try
        {
            InventoryResizePatch.ApplyRuntime(singleton);
            InventoryStackingFeature.ApplyRuntime();
            ScrollableInventoryFeature.ApplyRuntime();
            StorageContainer_Awake_Patch.ApplyRuntime();
            OxygenRefillFeature.ApplyRuntime();
            BaseGlassHullFeature.ApplyRuntime(singleton);
            BatteryItems.ApplyRuntime(singleton);
            CoralShellRegenFeature.ApplyRuntime();
        }
        catch (System.Exception ex)
        {
            QoLLog.Error(Category.Config, "ApplyRuntime after Save threw", ex);
        }
    }
}
