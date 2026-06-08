#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_ItemIcon), "RearrangeElements")]
internal static class uGUI_ItemIcon_RearrangeElements_StackBadgeOrder_Patch
{
	[HarmonyPostfix]
	private static void Postfix(uGUI_ItemIcon __instance)
	{
		StackIconHelper.BringStackBadgeToFront(__instance);
	}
}
