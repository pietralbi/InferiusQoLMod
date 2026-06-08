#nullable disable
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_QuickSlots), "Update")]
internal static class uGUI_QuickSlots_Update_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(uGUI_QuickSlots __instance)
	{
		if ((Object)(object)__instance == (Object)null)
		{
			return;
		}
		StackIconRefresher.CachedQuickSlotsUi = __instance;
		uGUI_ItemIcon[] value = Traverse.Create((object)__instance).Field<uGUI_ItemIcon[]>("icons").Value;
		object value2 = Traverse.Create((object)__instance).Method("GetTarget", Array.Empty<object>()).GetValue();
		IQuickSlots val = (IQuickSlots)((value2 is IQuickSlots) ? value2 : null);
		if (value == null || val == null)
		{
			return;
		}
		for (int i = 0; i < value.Length; i++)
		{
			uGUI_ItemIcon val2 = value[i];
			if (!((Object)(object)val2 == (Object)null))
			{
				InventoryItem slotItem = val.GetSlotItem(i);
				if ((Object)(object)((slotItem != null) ? slotItem.item : null) != (Object)null)
				{
					StackIconHelper.UpdateForPickup(val2, slotItem.item);
				}
			}
		}
	}
}
