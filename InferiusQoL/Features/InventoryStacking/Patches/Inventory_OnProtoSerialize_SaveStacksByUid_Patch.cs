#nullable disable
using System;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Inventory), "OnProtoSerialize", new Type[] { typeof(ProtobufSerializer) })]
[HarmonyPriority(800)]
internal static class Inventory_OnProtoSerialize_SaveStacksByUid_Patch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		StackUidPersistence.SaveCurrentSession();
	}
}
