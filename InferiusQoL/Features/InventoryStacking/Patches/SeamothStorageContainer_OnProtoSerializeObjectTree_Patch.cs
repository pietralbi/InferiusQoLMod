#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(SeamothStorageContainer), "OnProtoSerializeObjectTree")]
internal static class SeamothStorageContainer_OnProtoSerializeObjectTree_Patch
{
	[HarmonyPostfix]
	private static void Postfix(SeamothStorageContainer __instance)
	{
		StackUidPersistence.MergeCaptureContainerIntoSidecarFile((__instance != null) ? __instance.container : null);
	}
}
