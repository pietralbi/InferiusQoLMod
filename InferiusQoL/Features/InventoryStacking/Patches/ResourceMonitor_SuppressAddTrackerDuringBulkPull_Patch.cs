#nullable disable
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ResourceMonitor_SuppressAddTrackerDuringBulkPull_Patch
{
	public static bool Prepare()
	{
		if (ResourceMonitorCompat.IsAvailable)
		{
			return ResourceMonitorCompat.AddItemsToTrackerMethod != null;
		}
		return false;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		return ResourceMonitorCompat.AddItemsToTrackerMethod;
	}

	[HarmonyPrefix]
	private static bool Prefix()
	{
		return ResourceMonitorCompat.BulkPullDepth <= 0;
	}
}
