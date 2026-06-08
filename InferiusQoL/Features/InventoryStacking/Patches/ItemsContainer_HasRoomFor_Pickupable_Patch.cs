#nullable disable
using System;
using HarmonyLib;
using InferiusQoL.Features.LockerMover;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(ItemsContainer), "HasRoomFor", new Type[] { typeof(Pickupable) })]
internal static class ItemsContainer_HasRoomFor_Pickupable_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(Pickupable pickupable, ItemsContainer __instance, ref bool __result)
	{
		if ((!((Object)(object)Inventory.main != (Object)null) || __instance != Inventory.main.container || !uGUI.isIntro) && !LockerMoverClipboard.IsClipboardContainer(__instance) && !LifepodStorageScope.IsLifepodOrTimeCapsuleStorage(__instance) && !WaterParkStorageScope.IsCreatureHabitatContainer(__instance) && !__result && (Object)(object)pickupable != (Object)null && Stack.ContainerHasMergeRoomFor(__instance, pickupable))
		{
			__result = true;
		}
	}
}
