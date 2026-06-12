namespace InferiusQoL.Features.Batteries;

using HarmonyLib;
using InferiusQoL.Config;

[HarmonyPatch(typeof(Battery), "Awake")]
public static class Battery_Awake_CapacityPatch
{
    [HarmonyPostfix]
    public static void Postfix(Battery __instance)
    {
        var cfg = InferiusConfig.Instance;
        if (!cfg.BatteryReworkEnabled) return;

        var pickupable = __instance.GetComponent<Pickupable>();
        var techType = pickupable != null ? pickupable.GetTechType() : TechType.None;
        BatteryItems.ApplyConfiguredCapacity(__instance, techType, cfg, fullyCharge: false);
    }
}
