namespace InferiusQoL.Features.InventoryViewer;

using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// Init through Harmony postfix on Player.Awake, using the same pattern as
/// LockerMover. A GameObject created in BepInEx Plugin.Awake would end up in
/// DontDestroyOnLoad limbo.
/// </summary>
public static class InventoryViewerFeature
{
    private static GameObject? _hostGO;

    public static void Init() { /* deferred to Player.Awake */ }

    internal static void EnsureManager()
    {
        if (_hostGO != null) return;
        _hostGO = new GameObject("InferiusQoL_InventoryViewerManager");
        Object.DontDestroyOnLoad(_hostGO);
        _hostGO.AddComponent<InventoryViewerManager>();
        QoLLog.Info(Category.Inventory, "InventoryViewer initialized");
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
public static class InventoryViewer_Player_Awake_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!InferiusConfig.Instance.InventoryViewerEnabled) return;
        InventoryViewerFeature.EnsureManager();
    }
}
