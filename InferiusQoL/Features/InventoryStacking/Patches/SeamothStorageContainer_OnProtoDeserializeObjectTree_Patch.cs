#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(SeamothStorageContainer), "OnProtoDeserializeObjectTree")]
internal static class SeamothStorageContainer_OnProtoDeserializeObjectTree_Patch
{
	[HarmonyPostfix]
	private static void Postfix(SeamothStorageContainer __instance)
	{
		ItemsContainer container = ((__instance != null) ? __instance.container : null);
		StackUidPersistence.TryApplyContainerSidecar(container);
		StackUidPersistence.ScheduleSeamothDeserializeSidecarRetries(container);
	}
}
