#nullable disable
using System;
using System.Collections;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "OnProtoDeserializeAsync", new Type[] { typeof(ProtobufSerializer) })]
internal static class Inventory_OnProtoDeserializeAsync_LoadStacksByUid_Patch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		StackNotificationFix.MarkInventoryDeserializeStarted();
		StackUidPersistence.MarkUnloaded();
		StackUidPersistence.LoadForCurrentSession();
	}

	[HarmonyPostfix]
	private static IEnumerator Postfix(IEnumerator __result)
	{
		if (__result != null)
		{
			while (__result.MoveNext())
			{
				yield return __result.Current;
			}
			StackUidPersistence.ApplyPlayerInventorySidecarAfterDeserialize();
			StackNotificationFix.MarkInventoryDeserializeCompleted();
			StackIconRefresher.Trigger();
		}
	}
}
