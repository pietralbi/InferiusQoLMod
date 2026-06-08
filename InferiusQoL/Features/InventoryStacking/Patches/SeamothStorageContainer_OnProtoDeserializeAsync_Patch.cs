#nullable disable
using System.Collections;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(SeamothStorageContainer), "OnProtoDeserializeAsync")]
internal static class SeamothStorageContainer_OnProtoDeserializeAsync_Patch
{
	[HarmonyPostfix]
	private static IEnumerator Postfix(IEnumerator __result, SeamothStorageContainer __instance)
	{
		if (__result != null)
		{
			while (__result.MoveNext())
			{
				yield return __result.Current;
			}
			ItemsContainer container = ((__instance != null) ? __instance.container : null);
			StackUidPersistence.TryApplyContainerSidecar(container);
			StackUidPersistence.ScheduleSeamothDeserializeSidecarRetries(container);
		}
	}
}
