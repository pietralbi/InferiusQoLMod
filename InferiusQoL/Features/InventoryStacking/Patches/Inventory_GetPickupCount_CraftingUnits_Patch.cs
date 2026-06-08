#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "GetPickupCount")]
internal static class Inventory_GetPickupCount_CraftingUnits_Patch
{
	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool Prefix(TechType pickupType, ref int __result)
	{
		if (!CraftingCounts.InCraftingQuery || (Object)(object)Inventory.main == (Object)null)
		{
			return true;
		}
		__result = CraftingCounts.PickupUnitsForCraft(Inventory.main, pickupType);
		return false;
	}
}
