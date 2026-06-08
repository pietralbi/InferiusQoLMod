#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(NotificationManager), "Deserialize")]
internal static class NotificationManager_Deserialize_ReconcileInventory_Patch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		StackNotificationFix.AfterNotificationDeserialize();
	}
}
