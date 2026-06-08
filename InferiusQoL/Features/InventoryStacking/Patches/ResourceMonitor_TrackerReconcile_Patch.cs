#nullable disable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ResourceMonitor_TrackerReconcile_Patch
{
	public static bool Prepare()
	{
		return ResourceMonitorCompat.IsAvailable;
	}

	[HarmonyTargetMethods]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		if (ResourceMonitorCompat.AddItemsToTrackerMethod != null)
		{
			yield return ResourceMonitorCompat.AddItemsToTrackerMethod;
		}
		if (ResourceMonitorCompat.RemoveItemsFromTrackerMethod != null)
		{
			yield return ResourceMonitorCompat.RemoveItemsFromTrackerMethod;
		}
	}

	[HarmonyPostfix]
	private static void Postfix(object __instance, TechType item)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (ResourceMonitorCompat.BulkPullDepth <= 0)
		{
			ResourceMonitorCompat.ReconcileAndRefresh(__instance, item);
		}
	}
}
