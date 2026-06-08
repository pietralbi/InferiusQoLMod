#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(ItemDragManager), "InternalDragStart")]
internal static class ItemDragManager_InternalDragStart_Patch
{
	[HarmonyPostfix]
	private static void Postfix(InventoryItem item, ItemDragManager __instance)
	{
		if (!((Object)(object)((item != null) ? item.item : null) == (Object)null) && !((Object)(object)__instance == (Object)null))
		{
			uGUI_ItemIcon value = Traverse.Create((object)__instance).Field<uGUI_ItemIcon>("draggedIcon").Value;
			if ((Object)(object)value != (Object)null)
			{
				StackIconHelper.UpdateForPickup(value, item.item);
			}
		}
	}
}
