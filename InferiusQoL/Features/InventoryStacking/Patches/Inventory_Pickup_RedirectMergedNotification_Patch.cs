#nullable disable
using System;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "Pickup", new Type[] { typeof(Pickupable), typeof(bool) })]
internal static class Inventory_Pickup_RedirectMergedNotification_Patch
{
	[HarmonyPrefix]
	private static void Prefix(Pickupable pickupable)
	{
		StackNotificationFix.BeforeInventoryPickup(pickupable);
	}

	[HarmonyPostfix]
	private static void Postfix(Pickupable pickupable, bool noMessage, bool __result)
	{
		StackNotificationFix.AfterInventoryPickup(pickupable, __result, noMessage);
	}
}
