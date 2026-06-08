#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackNotificationFix
{
	private static readonly Dictionary<string, string> s_survivorUidByMergedUid = new Dictionary<string, string>(StringComparer.Ordinal);

	private static string s_activePickupUid;

	private static bool s_inventoryDeserializeCompleted;

	public static void RecordMergedRow(Pickupable mergedPickup, Pickupable survivorPickup)
	{
		if ((Object)(object)mergedPickup == (Object)null || mergedPickup == survivorPickup)
		{
			return;
		}
		if (!TryGetUid(mergedPickup, out string mergedUid))
		{
			return;
		}
		if (!string.Equals(mergedUid, s_activePickupUid, StringComparison.Ordinal))
		{
			return;
		}
		TryGetUid(survivorPickup, out string survivorUid);
		s_survivorUidByMergedUid[mergedUid] = survivorUid;
	}

	public static void BeforeInventoryPickup(Pickupable pickupable)
	{
		s_activePickupUid = TryGetUid(pickupable, out string uid) ? uid : null;
	}

	public static void AfterInventoryPickup(Pickupable pickupable, bool success, bool noMessage)
	{
		string mergedUid = TryGetUid(pickupable, out string uid) ? uid : s_activePickupUid;
		try
		{
			if (!success || noMessage || string.IsNullOrEmpty(mergedUid))
			{
				return;
			}
			if (!s_survivorUidByMergedUid.TryGetValue(mergedUid, out string survivorUid))
			{
				return;
			}
			NotificationManager.main.Remove(NotificationManager.Group.Inventory, mergedUid);
			if (!string.IsNullOrEmpty(survivorUid) && !string.Equals(survivorUid, mergedUid, StringComparison.Ordinal))
			{
				NotificationManager.main.Add(NotificationManager.Group.Inventory, survivorUid, 4f);
			}
		}
		finally
		{
			if (!string.IsNullOrEmpty(mergedUid))
			{
				s_survivorUidByMergedUid.Remove(mergedUid);
			}
			s_activePickupUid = null;
		}
	}

	public static void MarkInventoryDeserializeStarted()
	{
		s_inventoryDeserializeCompleted = false;
		s_survivorUidByMergedUid.Clear();
	}

	public static void MarkInventoryDeserializeCompleted()
	{
		s_inventoryDeserializeCompleted = true;
		RemoveOrphanedInventoryNotifications();
	}

	public static void AfterNotificationDeserialize()
	{
		if (s_inventoryDeserializeCompleted)
		{
			RemoveOrphanedInventoryNotifications();
		}
	}

	public static int RemoveOrphanedInventoryNotifications()
	{
		if ((Object)(object)Inventory.main == (Object)null || (Object)(object)NotificationManager._main == (Object)null)
		{
			return 0;
		}
		HashSet<string> liveUids = CollectPlayerItemUids();
		List<string> staleUids = new List<string>();
		foreach (KeyValuePair<NotificationManager.NotificationId, NotificationManager.NotificationData> notification in NotificationManager.main.notifications)
		{
			if (notification.Key.group == NotificationManager.Group.Inventory && !liveUids.Contains(notification.Key.key))
			{
				staleUids.Add(notification.Key.key);
			}
		}
		for (int i = 0; i < staleUids.Count; i++)
		{
			NotificationManager.main.Remove(NotificationManager.Group.Inventory, staleUids[i]);
		}
		if (staleUids.Count > 0)
		{
			InventoryStackingFeature.Log.LogInfo($"InventoryStacking removed {staleUids.Count} orphaned inventory notifications.");
		}
		return staleUids.Count;
	}

	private static HashSet<string> CollectPlayerItemUids()
	{
		HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
		Inventory inventory = Inventory.main;
		if ((Object)(object)inventory == (Object)null)
		{
			return result;
		}
		if (inventory.container != null)
		{
			foreach (InventoryItem item in inventory.container)
			{
				AddItemUid(result, item);
			}
		}
		if (inventory.equipment != null)
		{
			foreach (InventoryItem item in (IItemsContainer)inventory.equipment)
			{
				AddItemUid(result, item);
			}
		}
		return result;
	}

	private static void AddItemUid(HashSet<string> uids, InventoryItem item)
	{
		if ((Object)(object)((item != null) ? item.item : null) != (Object)null && TryGetUid(item.item, out string uid))
		{
			uids.Add(uid);
		}
	}

	private static bool TryGetUid(Pickupable pickupable, out string uid)
	{
		uid = null;
		if ((Object)(object)pickupable == (Object)null)
		{
			return false;
		}
		UniqueIdentifier component = ((Component)pickupable).GetComponent<UniqueIdentifier>();
		if ((Object)(object)component == (Object)null || string.IsNullOrEmpty(component.Id))
		{
			return false;
		}
		uid = component.Id;
		return true;
	}
}
