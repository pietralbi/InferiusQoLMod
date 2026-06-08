#nullable disable
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ItemsContainer_NotifyChangeItemPosition_TryMerge_Patch
{
	public static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(ItemsContainer), "NotifyChangeItemPosition", new Type[1] { typeof(InventoryItem) }, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix(ItemsContainer __instance, InventoryItem item)
	{
		if (__instance != null && !((Object)(object)((item != null) ? item.item : null) == (Object)null))
		{
			StackConsolidation.TryMerge(item, __instance);
		}
	}
}
