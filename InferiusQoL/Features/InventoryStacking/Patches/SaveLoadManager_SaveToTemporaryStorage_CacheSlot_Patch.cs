#nullable disable
using System;
using HarmonyLib;
using UnityEngine;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(SaveLoadManager), "SaveToTemporaryStorageAsync", new Type[] { typeof(Texture2D) })]
internal static class SaveLoadManager_SaveToTemporaryStorage_CacheSlot_Patch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		SaveSlotMetadata.CacheCurrentSlot();
	}
}
