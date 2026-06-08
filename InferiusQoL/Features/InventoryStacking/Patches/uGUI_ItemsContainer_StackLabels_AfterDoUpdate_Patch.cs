#nullable disable
using System.Collections.Generic;
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
		ItemsContainer value = Traverse.Create((object)__instance).Field<ItemsContainer>("container").Value;
		if (value == null)
		{
			return;
		}
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)value)
		{
			if (!((Object)(object)((item != null) ? item.item : null) == (Object)null))
			{
				uGUI_ItemIcon icon = __instance.GetIcon(item);
				if ((Object)(object)icon != (Object)null)
				{
					StackIconHelper.UpdateForPickup(icon, item.item);
				}
			}
		}
	}
}
