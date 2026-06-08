#nullable disable
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ResourceMonitor_AttemptToTakeItem_PerLockerPull_Patch
{
	public static bool Prepare()
	{
		if (ResourceMonitorCompat.IsAvailable)
		{
			return ResourceMonitorCompat.AttemptToTakeItemMethod != null;
		}
		return false;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		return ResourceMonitorCompat.AttemptToTakeItemMethod;
	}

	[HarmonyPrefix]
	internal static bool Prefix(object __instance, TechType item)
	{
		if (__instance == null || (Object)(object)Player.main == (Object)null || (Object)(object)Inventory.main == (Object)null)
		{
			return true;
		}
		if (ResourceMonitorCompat.IsBeingDeleted(__instance))
		{
			return true;
		}
		if (ResourceMonitorCompat.PickupCooldownField != null && (float)ResourceMonitorCompat.PickupCooldownField.GetValue(__instance) > 0f)
		{
			if (TryGetTrackedResource(__instance, item, out var trackedResource) && !TryPickLockerToDrain(trackedResource, item, out var _))
			{
				ResourceMonitorCompat.ReconcileAndRefresh(__instance, item);
			}
			return true;
		}
		if (!TryGetTrackedResource(__instance, item, out var trackedResource2))
		{
			return true;
		}
		if (!TryPickLockerToDrain(trackedResource2, item, out var locker2))
		{
			ResourceMonitorCompat.ReconcileAndRefresh(__instance, item);
			return true;
		}
		if (TakeAllFromLocker(__instance, item, locker2))
		{
			return false;
		}
		return true;
	}

	private static bool TakeAllFromLocker(object logic, TechType tech, StorageContainer locker)
	{
		if (((locker != null) ? locker.container : null) == null)
		{
			return false;
		}
		ItemsContainer container = locker.container;
		bool flag = false;
		MRStack.SuppressMerge = true;
		ResourceMonitorCompat.BeginBulkPull();
		try
		{
			while (container.Contains(tech))
			{
				Pickupable val = container.RemoveItem(tech);
				if ((Object)(object)val == (Object)null)
				{
					break;
				}
				if (Inventory.main.Pickup(val, false))
				{
					flag = true;
					continue;
				}
				container.AddItem(val);
				break;
			}
		}
		finally
		{
			ResourceMonitorCompat.EndBulkPull(logic, tech, flag);
			MRStack.SuppressMerge = false;
		}
		if (!flag)
		{
			return false;
		}
		if (!IsStillTracked(logic, tech) && ResourceMonitorCompat.PickupCooldownField != null)
		{
			ResourceMonitorCompat.PickupCooldownField.SetValue(logic, 0.7f);
		}
		StackIconRefresher.Trigger();
		return true;
	}

	private static bool TryGetTrackedResource(object logic, TechType item, out object trackedResource)
	{
		trackedResource = null;
		if (ResourceMonitorCompat.TrackedResourcesProperty == null)
		{
			return false;
		}
		if (!(ResourceMonitorCompat.TrackedResourcesProperty.GetValue(logic) is IDictionary dictionary) || !dictionary.Contains(item))
		{
			return false;
		}
		trackedResource = dictionary[item];
		return trackedResource != null;
	}

	private static bool TryPickLockerToDrain(object trackedResource, TechType tech, out StorageContainer locker)
	{
		locker = null;
		if (ResourceMonitorCompat.ContainersProperty == null)
		{
			return false;
		}
		if (!(ResourceMonitorCompat.ContainersProperty.GetValue(trackedResource) is IEnumerable enumerable))
		{
			return false;
		}
		foreach (object item in enumerable)
		{
			StorageContainer val = (StorageContainer)((item is StorageContainer) ? item : null);
			if (val != null && val.container != null && val.container.Contains(tech))
			{
				locker = val;
				return true;
			}
		}
		return false;
	}

	private static bool IsStillTracked(object logic, TechType item)
	{
		if (ResourceMonitorCompat.TrackedResourcesProperty == null)
		{
			return false;
		}
		if (ResourceMonitorCompat.TrackedResourcesProperty.GetValue(logic) is IDictionary dictionary)
		{
			return dictionary.Contains(item);
		}
		return false;
	}
}
