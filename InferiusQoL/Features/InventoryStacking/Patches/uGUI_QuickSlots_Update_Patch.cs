#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_QuickSlots), "Update")]
internal static class uGUI_QuickSlots_Update_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(uGUI_QuickSlots __instance)
	{
		if ((Object)(object)__instance == (Object)null)
		{
			return;
		}
		StackIconRefresher.RefreshQuickSlotsFromUpdate(__instance);
	}
}
