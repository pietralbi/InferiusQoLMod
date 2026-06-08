#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_EquipmentSlot), "SetItem")]
internal static class uGUI_EquipmentSlot_SetItem_Patch
{
	[HarmonyPostfix]
	private static void Postfix(InventoryItem item, uGUI_EquipmentSlot __instance)
	{
		if (!((Object)(object)((item != null) ? item.item : null) == (Object)null) && !((Object)(object)__instance == (Object)null))
		{
			StackIconHelper.UpdateForPickup(__instance.icon, item.item);
		}
	}
}
