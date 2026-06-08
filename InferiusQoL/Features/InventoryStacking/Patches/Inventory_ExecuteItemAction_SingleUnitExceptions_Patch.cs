#nullable disable
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "ExecuteItemAction", new Type[]
{
	typeof(ItemAction),
	typeof(InventoryItem)
})]
internal static class Inventory_ExecuteItemAction_SingleUnitExceptions_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(ItemAction action, InventoryItem item)
	{
		if ((Object)(object)((item != null) ? item.item : null) == (Object)null || (Object)(object)Inventory.main == (Object)null)
		{
			return true;
		}
		if (!StackRules.CanStack(item.item))
		{
			return true;
		}
		if (Stack.CountOf(item.item) <= 1)
		{
			return true;
		}
		if (PartialTransferOne.TryConsumeOneFromStack(item, action))
		{
			return false;
		}
		if ((int)action == 128)
		{
			PartialTransferOne.TryDropOne(item);
			return false;
		}
		if ((int)action == 32 && ReactorFeedHelper.IsReactorContainer(Inventory.main.GetOppositeContainer(item)))
		{
			if (PartialTransferOne.TryStart(item))
			{
				return false;
			}
			return true;
		}
		return true;
	}
}
