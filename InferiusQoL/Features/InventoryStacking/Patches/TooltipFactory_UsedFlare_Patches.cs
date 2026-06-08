#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(TooltipFactory), "InventoryItem")]
internal static class TooltipFactory_InventoryItem_UsedFlare_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(InventoryItem item, TooltipData data)
	{
		if (!StackFlareState.IsUsedFlare((item != null) ? item.item : null))
		{
			return true;
		}

		StackFlareState.WriteUsedFlareTooltip(data, item.item, item, includeActions: true);
		return false;
	}
}

[HarmonyPatch(typeof(TooltipFactory), "InventoryItemView")]
internal static class TooltipFactory_InventoryItemView_UsedFlare_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(InventoryItem item, TooltipData data)
	{
		if (!StackFlareState.IsUsedFlare((item != null) ? item.item : null))
		{
			return true;
		}

		StackFlareState.WriteUsedFlareTooltip(data, item.item);
		return false;
	}
}

[HarmonyPatch(typeof(TooltipFactory), "QuickSlot")]
internal static class TooltipFactory_QuickSlot_UsedFlare_Patch
{
	[HarmonyPrefix]
	private static bool Prefix(TechType techType, GameObject obj, TooltipData data)
	{
		if (!StackFlareState.IsFlareTech(techType) || (Object)(object)obj == (Object)null)
		{
			return true;
		}

		Pickupable pickupable = obj.GetComponent<Pickupable>();
		if (!StackFlareState.IsUsedFlare(pickupable))
		{
			return true;
		}

		StackFlareState.WriteUsedFlareTooltip(data, pickupable);
		return false;
	}
}
