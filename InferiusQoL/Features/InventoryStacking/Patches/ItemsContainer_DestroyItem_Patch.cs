#nullable disable
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(ItemsContainer), "DestroyItem")]
internal static class ItemsContainer_DestroyItem_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(TechType techType, ItemsContainer __instance, ref bool __result)
	{
		if ((Object)(object)Inventory.main != (Object)null && __instance == Inventory.main.container && uGUI.isIntro)
		{
			return true;
		}
		IList<InventoryItem> items = __instance.GetItems(techType);
		if (items == null || items.Count == 0)
		{
			__result = false;
			return false;
		}
		for (int i = 0; i < items.Count; i++)
		{
			InventoryItem val = items[i];
			if ((Object)(object)((val != null) ? val.item : null) != (Object)null && ((Component)val.item).GetComponent<IBattery>() != null)
			{
				return true;
			}
		}
		for (int num = items.Count - 1; num >= 0; num--)
		{
			InventoryItem val2 = items[num];
			if (!((Object)(object)((val2 != null) ? val2.item : null) == (Object)null))
			{
				Pickupable item = val2.item;
				if (StackRules.CanStack(item))
				{
					if (MRStack.CountOf(item) > 1)
					{
						MRStack.Add(item, -1);
						__result = true;
						return false;
					}
					if (__instance.RemoveItem(item, true))
					{
						Object.Destroy((Object)(object)((Component)item).gameObject);
						__result = true;
						return false;
					}
				}
				else if (__instance.RemoveItem(item, true))
				{
					Object.Destroy((Object)(object)((Component)item).gameObject);
					__result = true;
					return false;
				}
			}
		}
		__result = false;
		return false;
	}
}
