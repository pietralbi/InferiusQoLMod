#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_ItemsContainer), "DoUpdate")]
internal static class uGUI_ItemsContainer_StackLabels_AfterDoUpdate_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(uGUI_ItemsContainer __instance)
	{
		if ((Object)(object)__instance == (Object)null)
		{
			return;
		}
		StackIconRefresher.RefreshViewFromDoUpdate(__instance);
	}
}
