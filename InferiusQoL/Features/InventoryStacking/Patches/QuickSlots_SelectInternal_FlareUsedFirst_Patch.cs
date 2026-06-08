#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(QuickSlots), "SelectInternal")]
internal static class QuickSlots_SelectInternal_FlareUsedFirst_Patch
{
	[HarmonyPrefix]
	private static void Prefix(QuickSlots __instance, int slotID)
	{
		if (__instance == null || slotID < 0 || slotID >= __instance.slotCount)
		{
			return;
		}

		InventoryItem current = __instance.GetSlotItem(slotID);
		if ((Object)(object)((current != null) ? current.item : null) == (Object)null || !StackFlareState.IsUnusedFlare(current.item))
		{
			return;
		}

		InventoryItem usedFlare = StackFlareState.FindUsedInventoryFlare(__instance, requireUnbound: true);
		if (usedFlare == null || usedFlare == current)
		{
			return;
		}

		__instance.Unbind(slotID);
		__instance.Bind(slotID, usedFlare);
	}
}
