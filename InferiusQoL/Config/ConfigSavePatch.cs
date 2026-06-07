namespace InferiusQoL.Config;

using System.Reflection;
using HarmonyLib;
using InferiusQoL.Features.InventoryResize;
using InferiusQoL.Features.LockerResize;
using InferiusQoL.Features.OxygenRefill;
using InferiusQoL.Features.ScrollableInventory;
using InferiusQoL.Logging;
using Nautilus.Json;

/// <summary>
/// Postfix na ConfigFile.Save(). Vola se kdykoliv Nautilus ulozi config po zmene
/// slideru/toggle v Options menu (diky SaveOn = ChangeValue). Zajistuje, ze po
/// zmene configu se runtime aplikuji featury, ktere umi zmenu absorbovat za behu.
///
/// DULEZITE: Nautilus pri save muze pouzit instanci configu ktera NENI nas
/// singleton a ma DEFAULT hodnoty pro pole ktera uzivatel nemenil v tomto save.
/// Kdybychom predali __instance do ApplyRuntime, defaults by prepsaly user-set
/// values z singletonu (napr. posun range slideru by resetovali inventar na
/// vanilla velikost). Proto merge: zmenene pole z __instance -> singleton,
/// potom ApplyRuntime vola se SINGLETONEM ktery je now updated.
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

        // Non-singleton: reload singletonu z JSON (kterou Nautilus prave zapsal)
        // aby singleton mel VSECHNY aktualni user hodnoty + noveho zmenene pole.
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
            ScrollableInventoryFeature.ApplyRuntime();
            StorageContainer_Awake_Patch.ApplyRuntime();
            OxygenRefillFeature.ApplyRuntime();
        }
        catch (System.Exception ex)
        {
            QoLLog.Error(Category.Config, "ApplyRuntime after Save threw", ex);
        }
    }
}
