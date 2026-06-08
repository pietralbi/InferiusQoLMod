#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(QuickSlots), "TryRefill")]
internal static class QuickSlots_TryRefill_FlareUsedFirst_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(QuickSlots __instance, TechType techType, int slotID, ref bool __result)
	{
		if (__instance == null || !StackFlareState.IsFlareTech(techType))
		{
			return true;
		}

		InventoryItem usedFlare = StackFlareState.FindUsedInventoryFlare(__instance, requireUnbound: true);
		if (usedFlare == null)
		{
			return true;
		}

		__instance.Bind(slotID, usedFlare);
		__result = true;
		return false;
	}
}
