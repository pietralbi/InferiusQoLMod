#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

using System.Collections;
using BepInEx.Logging;
using InferiusQoL.Config;
using InferiusQoL.Features.InventoryStacking.Patches;
using InferiusQoL.Logging;
using UnityEngine;

internal static class InventoryStackingFeature
{
    internal static bool IsEnabled =>
        InferiusConfig.Instance.InventoryStackingEnabled
        && !global::InferiusQoL.Plugin.HasInventoryStacking;

    internal static ManualLogSource Log => global::InferiusQoL.Plugin.Logger;

    public static void Init()
    {
        if (!IsEnabled) return;

        PartialTransferOne.ClearSingleUnitTargetCache();
        MrProtoRegistration.TryRegister(Log);

        var host = global::InferiusQoL.Plugin.Instance;
        host.StartCoroutine(CoRetryProtoRegister());
        host.StartCoroutine(CoRetryResourceMonitorPatches());

        QoLLog.Info(Category.Inventory, $"InventoryStacking active (max stack: {StackConfig.MaxStackSize})");
    }

    public static void AfterHarmonyPatched()
    {
        if (!IsEnabled) return;

        HarmonyPatchDiagnostics.LogCraftingAndStackingPatches(Log);
    }

    public static void ApplyRuntime()
    {
        if (!IsEnabled) return;

        StackCapEnforcer.EnforceCapOnAllPlayerInventories();
    }

    private static IEnumerator CoRetryProtoRegister()
    {
        yield return new WaitForSecondsRealtime(2f);
        MrProtoRegistration.TryRegister(Log);
    }

    private static IEnumerator CoRetryResourceMonitorPatches()
    {
        yield return new WaitForSecondsRealtime(2f);
        ResourceMonitorCompat.TryApplyLatePatches(global::InferiusQoL.Plugin.Harmony, Log);
        HarmonyPatchDiagnostics.LogResourceMonitorPatches(Log);
    }
}

internal static class Plugin
{
    internal static global::InferiusQoL.Plugin Instance => global::InferiusQoL.Plugin.Instance;

    internal static ManualLogSource Log => global::InferiusQoL.Plugin.Logger;
}
