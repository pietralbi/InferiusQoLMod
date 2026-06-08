#nullable disable
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ResourceMonitor_SuppressTrackerDuringBulkPull_Patch
{
	public static bool Prepare()
	{
		if (ResourceMonitorCompat.IsAvailable)
		{
			return ResourceMonitorCompat.RemoveItemsFromTrackerMethod != null;
		}
		return false;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		return ResourceMonitorCompat.RemoveItemsFromTrackerMethod;
	}

	[HarmonyPrefix]
	private static bool Prefix()
	{
		return ResourceMonitorCompat.BulkPullDepth <= 0;
	}
}
