namespace InferiusQoL.Features.LockerMover;

using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// Facade for registering the LockerMover feature. Attach the manager MonoBehaviour
/// only on Player.Awake (postfix patch below), because a GameObject created in
/// BepInEx Plugin.Awake before scene load ends up in DontDestroyOnLoad limbo:
/// Awake fires, but Update does not tick.
/// </summary>
public static class LockerMoverFeature
{
    private static GameObject? _hostGO;

    public static void Init()
    {
        // The actual manager creation is deferred to the Harmony patch on Player.Awake.
        // A GameObject created in BepInEx Plugin.Awake before scene load ends up in
        // DontDestroyOnLoad limbo: Awake fires, but Update does not tick.
    }

    internal static void EnsureManager()
    {
        if (_hostGO != null) return;

        _hostGO = new GameObject("InferiusQoL_LockerMoverManager");
        Object.DontDestroyOnLoad(_hostGO);
        _hostGO.AddComponent<LockerMoverManager>();
        QoLLog.Info(Category.LockerMover, "LockerMover initialized");
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
public static class LockerMoverPlayerAwakePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!InferiusConfig.Instance.LockerMoverEnabled) return;
        LockerMoverFeature.EnsureManager();
    }
}
