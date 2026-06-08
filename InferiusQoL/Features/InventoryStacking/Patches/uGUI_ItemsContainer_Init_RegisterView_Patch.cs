#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_ItemsContainer), "Init")]
internal static class uGUI_ItemsContainer_Init_RegisterView_Patch
{
	[HarmonyPostfix]
	private static void Postfix(uGUI_ItemsContainer __instance)
	{
		ItemsContainerViewRegistry.Register(__instance);
	}
}
