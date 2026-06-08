#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_ItemsContainer), "Uninit")]
internal static class uGUI_ItemsContainer_Uninit_UnregisterView_Patch
{
	[HarmonyPrefix]
	private static void Prefix(uGUI_ItemsContainer __instance)
	{
		ItemsContainerViewRegistry.Unregister(__instance);
	}
}
