#nullable disable
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "DestroyItem", new Type[]
{
	typeof(TechType),
	typeof(bool)
})]
internal static class Inventory_DestroyItem_ReactorRodSingleUnit_Patch
{
	public static bool Prepare()
	{
		return false;
	}

	[HarmonyPrefix]
	private static bool Prefix(TechType destroyTechType, bool allowGenerics, Inventory __instance, ref bool __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)destroyTechType != 64)
		{
			return true;
		}
		ItemsContainer val = ((__instance != null) ? __instance.container : null);
		if (val == null)
		{
			return true;
		}
		IList<InventoryItem> items = val.GetItems((TechType)64);
		if (items == null || items.Count == 0)
		{
			__result = false;
			return false;
		}
		for (int num = items.Count - 1; num >= 0; num--)
		{
			InventoryItem obj = items[num];
			Pickupable val2 = ((obj != null) ? obj.item : null);
			if (!((Object)(object)val2 == (Object)null))
			{
				if (MRStack.CountOf(val2) > 1)
				{
					MRStack.Add(val2, -1);
					__result = true;
					return false;
				}
				if (val.RemoveItem(val2, true))
				{
					Object.Destroy((Object)(object)((Component)val2).gameObject);
					__result = true;
					return false;
				}
			}
		}
		__result = false;
		return false;
	}
}
