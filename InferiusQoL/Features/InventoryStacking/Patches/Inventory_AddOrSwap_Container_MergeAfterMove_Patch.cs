#nullable disable
using System;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "AddOrSwap", new Type[]
{
	typeof(InventoryItem),
	typeof(IItemsContainer)
})]
internal static class Inventory_AddOrSwap_Container_MergeAfterMove_Patch
{
	[HarmonyPostfix]
	private static void Postfix(InventoryItem itemA, ref bool __result)
	{
		InventorySwapPatchHelper.TryMergeMovedItem(__result, itemA);
	}
}
