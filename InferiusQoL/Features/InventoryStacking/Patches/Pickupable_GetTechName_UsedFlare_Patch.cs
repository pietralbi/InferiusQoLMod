#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Pickupable), "GetTechName")]
internal static class Pickupable_GetTechName_UsedFlare_Patch
{
	[HarmonyPostfix]
	private static void Postfix(Pickupable __instance, ref string __result)
	{
		if (StackFlareState.IsUsedFlare(__instance))
		{
			__result = StackFlareState.UsedFlareName;
		}
	}
}
