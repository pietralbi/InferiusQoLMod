#nullable disable
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(ItemsContainer), "RemoveItem", new Type[]
{
	typeof(Pickupable),
	typeof(bool)
})]
internal static class ItemsContainer_RemoveItem_Pickupable_ReactorRodSplit_Patch
{
	public static bool Prepare()
	{
		return true;
	}

	[HarmonyPrefix]
	private static bool Prefix(ItemsContainer __instance, Pickupable pickupable, bool forced, ref bool __result)
	{
		if (__instance == null || (Object)(object)pickupable == (Object)null)
		{
			return true;
		}
		if ((int)pickupable.GetTechType() != 64)
		{
			return true;
		}
		if ((Object)(object)Inventory.main == (Object)null || __instance != Inventory.main.container)
		{
			return true;
		}
		if (!forced && !((IItemsContainer)__instance).AllowedToRemove(pickupable, true))
		{
			__result = false;
			return false;
		}
		if (MRStack.CountOf(pickupable) <= 1)
		{
			return true;
		}
		MRStack.Add(pickupable, -1);
		__result = true;
		return false;
	}
}
