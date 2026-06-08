#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

internal static class ResourceMonitorCompat
{
	internal const float LastItemPickupCooldownSeconds = 0.7f;

	internal static int BulkPullDepth;

	private static Type _logicType;

	private static Type _trackedResourceType;

	private static Type _displayType;

	private static PropertyInfo _trackedResourcesProperty;

	private static PropertyInfo _amountProperty;

	private static PropertyInfo _containersProperty;

	private static FieldInfo _displayField;

	private static FieldInfo _pickupCooldownField;

	private static FieldInfo _isBeingDeletedField;

	private static MethodInfo _itemModifiedMethod;

	private static MethodInfo _attemptToTakeItemMethod;

	private static MethodInfo _addItemsMethod;

	private static MethodInfo _removeItemsMethod;

	internal static Type LogicType
	{
		get
		{
			object obj = _logicType;
			if (obj == null)
			{
				obj = AccessTools.TypeByName("ResourceMonitor.Components.ResourceMonitorLogic") ?? AccessTools.TypeByName("ResourceMonitorLogic");
				_logicType = (Type)obj;
			}
			return (Type)obj;
		}
	}

	internal static bool IsAvailable
	{
		get
		{
			if (LogicType != null && TrackedResourceType != null && TrackedResourcesProperty != null && AmountProperty != null)
			{
				return ContainersProperty != null;
			}
			return false;
		}
	}

	private static Type TrackedResourceType
	{
		get
		{
			object obj = _trackedResourceType;
			if (obj == null)
			{
				obj = AccessTools.TypeByName("ResourceMonitor.Components.TrackedResource") ?? AccessTools.TypeByName("TrackedResource");
				_trackedResourceType = (Type)obj;
			}
			return (Type)obj;
		}
	}

	private static Type DisplayType
	{
		get
		{
			object obj = _displayType;
			if (obj == null)
			{
				obj = AccessTools.TypeByName("ResourceMonitor.Components.ResourceMonitorDisplay") ?? AccessTools.TypeByName("ResourceMonitorDisplay");
				_displayType = (Type)obj;
			}
			return (Type)obj;
		}
	}

	internal static PropertyInfo TrackedResourcesProperty => _trackedResourcesProperty ?? (_trackedResourcesProperty = LogicType?.GetProperty("TrackedResources", BindingFlags.Instance | BindingFlags.Public));

	private static PropertyInfo AmountProperty => _amountProperty ?? (_amountProperty = TrackedResourceType?.GetProperty("Amount"));

	internal static PropertyInfo ContainersProperty => _containersProperty ?? (_containersProperty = TrackedResourceType?.GetProperty("Containers"));

	private static FieldInfo DisplayField => _displayField ?? (_displayField = LogicType?.GetField("rmd", BindingFlags.Instance | BindingFlags.NonPublic));

	internal static FieldInfo PickupCooldownField => _pickupCooldownField ?? (_pickupCooldownField = LogicType?.GetField("timerTillNextPickup", BindingFlags.Instance | BindingFlags.NonPublic));

	private static FieldInfo IsBeingDeletedField => _isBeingDeletedField ?? (_isBeingDeletedField = LogicType?.GetField("IsBeingDeleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

	private static MethodInfo ItemModifiedMethod => _itemModifiedMethod ?? (_itemModifiedMethod = DisplayType?.GetMethod("ItemModified", BindingFlags.Instance | BindingFlags.Public, null, new Type[2]
	{
		typeof(TechType),
		typeof(int)
	}, null));

	internal static MethodInfo AttemptToTakeItemMethod => _attemptToTakeItemMethod ?? (_attemptToTakeItemMethod = ((LogicType == null) ? null : AccessTools.Method(LogicType, "AttemptToTakeItem", new Type[1] { typeof(TechType) }, (Type[])null)));

	internal static MethodInfo AddItemsToTrackerMethod => _addItemsMethod ?? (_addItemsMethod = ((LogicType == null) ? null : AccessTools.Method(LogicType, "AddItemsToTracker", new Type[3]
	{
		typeof(StorageContainer),
		typeof(TechType),
		typeof(int)
	}, (Type[])null)));

	internal static MethodInfo RemoveItemsFromTrackerMethod => _removeItemsMethod ?? (_removeItemsMethod = ((LogicType == null) ? null : AccessTools.Method(LogicType, "RemoveItemsFromTracker", new Type[3]
	{
		typeof(StorageContainer),
		typeof(TechType),
		typeof(int)
	}, (Type[])null)));

	internal static void TryApplyLatePatches(Harmony harmony, ManualLogSource log)
	{
		if (!IsAvailable || AttemptToTakeItemMethod == null)
		{
			return;
		}
		MethodInfo attemptToTakeItemMethod = AttemptToTakeItemMethod;
		HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo((MethodBase)attemptToTakeItemMethod);
		if (patchInfo != null && patchInfo.Prefixes?.Any((Patch p) => p.owner == Plugin.HarmonyId) == true)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)attemptToTakeItemMethod, new HarmonyMethod(typeof(ResourceMonitor_AttemptToTakeItem_PerLockerPull_Patch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			if (log != null)
			{
				log.LogInfo((object)"[InventoryStacking] Resource Monitor take patch applied (late bind).");
			}
		}
		catch (Exception ex)
		{
			if (log != null)
			{
				log.LogWarning((object)("[InventoryStacking] Resource Monitor late patch failed: " + ex.Message));
			}
		}
	}

	internal static bool IsBeingDeleted(object logic)
	{
		if (logic == null || IsBeingDeletedField == null)
		{
			return false;
		}
		object value = IsBeingDeletedField.GetValue(logic);
		bool flag = default(bool);
		int num;
		if (value is bool)
		{
			flag = (bool)value;
			num = 1;
		}
		else
		{
			num = 0;
		}
		return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
	}

	internal static void BeginBulkPull()
	{
		BulkPullDepth++;
	}

	internal static void EndBulkPull(object logic, TechType item, bool reconcile)
	{
		if (BulkPullDepth > 0)
		{
			BulkPullDepth--;
		}
		if (reconcile && BulkPullDepth == 0)
		{
			ReconcileAndRefresh(logic, item);
		}
	}

	internal static void ReconcileAndRefresh(object logic, TechType item)
	{
		if (logic == null || TrackedResourcesProperty == null || AmountProperty == null || ContainersProperty == null)
		{
			return;
		}
		CraftingCounts.InvalidateCache();
		if (!(TrackedResourcesProperty.GetValue(logic) is IDictionary dictionary) || !dictionary.Contains(item))
		{
			return;
		}
		object obj = dictionary[item];
		object value = ContainersProperty.GetValue(obj);
		if (!(value is IEnumerable enumerable))
		{
			return;
		}
		int num = 0;
		List<StorageContainer> list = new List<StorageContainer>();
		foreach (object item2 in enumerable)
		{
			StorageContainer val = (StorageContainer)((item2 is StorageContainer) ? item2 : null);
			if (val != null && val.container != null)
			{
				int num2 = CraftingCounts.UnitsIn(val.container, item);
				if (num2 <= 0)
				{
					list.Add(val);
				}
				else
				{
					num += num2;
				}
			}
		}
		if (list.Count > 0)
		{
			PruneContainers(value, list);
		}
		if (num <= 0)
		{
			dictionary.Remove(item);
			RefreshDisplay(logic, item, 0);
		}
		else
		{
			AmountProperty.SetValue(obj, num, null);
			RefreshDisplay(logic, item, num);
		}
	}

	private static void PruneContainers(object containersObj, List<StorageContainer> emptyLockers)
	{
		if (containersObj is ICollection<StorageContainer> collection)
		{
			{
				foreach (StorageContainer emptyLocker in emptyLockers)
				{
					collection.Remove(emptyLocker);
				}
				return;
			}
		}
		MethodInfo method = containersObj.GetType().GetMethod("Remove", new Type[1] { typeof(StorageContainer) });
		if (method == null)
		{
			return;
		}
		foreach (StorageContainer emptyLocker2 in emptyLockers)
		{
			method.Invoke(containersObj, new object[1] { emptyLocker2 });
		}
	}

	private static void RefreshDisplay(object logic, TechType item, int amount)
	{
		object obj = ResolveDisplay(logic);
		obj?.GetType().GetMethod("ItemModified", BindingFlags.Instance | BindingFlags.Public, null, new Type[2]
		{
			typeof(TechType),
			typeof(int)
		}, null)?.Invoke(obj, new object[2] { item, amount });
	}

	private static object ResolveDisplay(object logic)
	{
		if (logic == null)
		{
			return null;
		}
		object obj = logic.GetType().GetField("rmd", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(logic);
		if (obj != null && obj != null)
		{
			return obj;
		}
		MonoBehaviour val = (MonoBehaviour)((logic is MonoBehaviour) ? logic : null);
		if (val != null && DisplayType != null)
		{
			return ((Component)val).GetComponent(DisplayType);
		}
		if (DisplayField != null)
		{
			return DisplayField.GetValue(logic);
		}
		return null;
	}
}
