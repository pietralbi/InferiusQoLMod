namespace InferiusQoL.Features.TankWelder;

using HarmonyLib;
using InferiusQoL.Logging;

/// <summary>
/// Postfix patch on UnderwaterMotor.AlterMaxSpeed. When the player has the T4
/// merged tank equipped (lightweight variant), add back the vanilla Plasteel speed
/// penalty as compensation, effectively canceling the speed reduction from the
/// equipped tank.
/// </summary>
[HarmonyPatch(typeof(UnderwaterMotor), nameof(UnderwaterMotor.AlterMaxSpeed))]
public static class UnderwaterMotor_AlterMaxSpeed_Patch
{
    /// <summary>Vanilla PlasteelTank speed penalty (m/s). Estimate, refined by testing.</summary>
    private const float PlasteelTankSpeedPenalty = 0.45f;

    [HarmonyPostfix]
    public static void Postfix(UnderwaterMotor __instance, ref float __result)
    {
        var inv = Inventory.main;
        if (inv?.equipment == null) return;

        if (TankWelderItems.MergedTankT4 != TechType.None
            && inv.equipment.GetCount(TankWelderItems.MergedTankT4) > 0)
        {
            __result += PlasteelTankSpeedPenalty;
        }
    }
}
