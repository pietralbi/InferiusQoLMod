#nullable disable
using System.Collections.Generic;
using InferiusQoL.Features.LockerMover;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackConsolidation
{
	private static readonly Dictionary<int, float> s_mergeProtectedUntilTime = new Dictionary<int, float>();

	public static void ProtectFromMerge(Pickupable pickupable, float seconds)
	{
		if (!((Object)(object)pickupable == (Object)null))
		{
			int instanceID = ((Object)((Component)pickupable).gameObject).GetInstanceID();
			float value = Time.time + Mathf.Max(0.01f, seconds);
			s_mergeProtectedUntilTime[instanceID] = value;
		}
	}

	private static bool IsMergeProtected(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null)
		{
			return false;
		}
		int instanceID = ((Object)((Component)pickupable).gameObject).GetInstanceID();
		if (!s_mergeProtectedUntilTime.TryGetValue(instanceID, out var value))
		{
			return false;
		}
		if (Time.time > value)
		{
			s_mergeProtectedUntilTime.Remove(instanceID);
			return false;
		}
		return true;
	}

	public static void AfterUnsafeAdd(InventoryItem added, ItemsContainer container)
	{
		TryMerge(added, container);
	}

	private static bool ShouldSkipPlayerPocketMerge(ItemsContainer container)
	{
		if ((Object)(object)Inventory.main != (Object)null && container == Inventory.main.container)
		{
			return uGUI.isIntro;
		}
		return false;
	}

	private static bool ShouldSkipContainerMerge(ItemsContainer container)
	{
		if (container == null)
		{
			return true;
		}
		return ShouldSkipPlayerPocketMerge(container)
			|| LockerMoverClipboard.IsClipboardContainer(container)
			|| LifepodStorageScope.IsLifepodOrTimeCapsuleStorage(container)
			|| WaterParkStorageScope.IsCreatureHabitatContainer(container)
			|| PartialTransferOne.IsSingleUnitInsertTarget((IItemsContainer)(object)container);
	}

	internal static void TryMerge(InventoryItem added, ItemsContainer container)
	{
		if (MRStack.SuppressMerge || (Object)(object)((added != null) ? added.item : null) == (Object)null || !StackRules.CanStack(added.item) || container == null || IsMergeProtected(added.item) || ShouldSkipContainerMerge(container))
		{
			return;
		}
		TechType techType = added.item.GetTechType();
		int num = MRStack.CountOf(added.item);
		if (num < 1)
		{
			return;
		}
		int maxStackSize = StackConfig.MaxStackSize;
		while (num > 0)
		{
			IList<InventoryItem> items = container.GetItems(techType);
			if (items == null || items.Count == 0)
			{
				break;
			}
			List<InventoryItem> list = new List<InventoryItem>(items);
			bool flag = false;
			foreach (InventoryItem item in list)
			{
				if (item == null || item == added || (Object)(object)item.item == (Object)null)
				{
					continue;
				}
				if (item.item == added.item)
				{
					RemoveMergedInventoryRow(added, container, item.item);
					StackIconRefresher.Trigger();
					return;
				}
				if (item.item.GetTechType() != techType || !StackRules.CanStack(item.item) || IsMergeProtected(item.item))
				{
					continue;
				}
				int num2 = MRStack.CountOf(item.item);
				if (num2 >= maxStackSize)
				{
					continue;
				}
				int num3 = Mathf.Min(maxStackSize - num2, num);
				if (num3 > 0)
				{
					MRStack.Add(item.item, num3);
					num -= num3;
					flag = true;
					if (num <= 0)
					{
						RemoveMergedInventoryRow(added, container, item.item);
						StackIconRefresher.Trigger();
						return;
					}
					MRStack.SetAmount(added.item, num);
					StackIconRefresher.Trigger();
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
	}

	internal static bool TryMergeAllLike(InventoryItem anchor)
	{
		if ((Object)(object)((anchor != null) ? anchor.item : null) == (Object)null || !StackRules.CanStack(anchor.item))
		{
			return false;
		}
		ItemsContainer container = anchor.container as ItemsContainer;
		if (ShouldSkipContainerMerge(container))
		{
			return false;
		}
		TechType techType = anchor.item.GetTechType();
		IList<InventoryItem> items = container.GetItems(techType);
		if (items == null || items.Count <= 1)
		{
			return false;
		}
		List<InventoryItem> rows = new List<InventoryItem>();
		int total = 0;
		foreach (InventoryItem item in items)
		{
			if ((Object)(object)((item != null) ? item.item : null) == (Object)null || item.item.GetTechType() != techType || !StackRules.CanStack(item.item))
			{
				continue;
			}
			rows.Add(item);
			total += Mathf.Max(1, MRStack.CountOf(item.item));
		}
		if (rows.Count <= 1 || total <= 0)
		{
			return false;
		}
		rows.Sort((a, b) => CompareMergeRows(anchor, a, b));
		int maxStackSize = StackConfig.MaxStackSize;
		int remaining = total;
		int removedRows = 0;
		for (int i = 0; i < rows.Count; i++)
		{
			InventoryItem row = rows[i];
			if ((Object)(object)((row != null) ? row.item : null) == (Object)null)
			{
				continue;
			}
			if (remaining > 0)
			{
				int rowsLeft = rows.Count - i;
				int amount = (rowsLeft == 1) ? remaining : Mathf.Min(maxStackSize, remaining);
				MRStack.SetAmount(row.item, amount);
				ClearMergeProtection(row.item);
				remaining -= amount;
				continue;
			}
			RemoveMergedInventoryRow(row, container, anchor.item);
			removedRows++;
		}
		if (removedRows <= 0)
		{
			return false;
		}
		StackIconRefresher.Trigger();
		return true;
	}

	private static int CompareMergeRows(InventoryItem anchor, InventoryItem a, InventoryItem b)
	{
		if (a == b)
		{
			return 0;
		}
		if (a == anchor)
		{
			return -1;
		}
		if (b == anchor)
		{
			return 1;
		}
		int y = a.y.CompareTo(b.y);
		if (y != 0)
		{
			return y;
		}
		int x = a.x.CompareTo(b.x);
		if (x != 0)
		{
			return x;
		}
		return a.GetHashCode().CompareTo(b.GetHashCode());
	}

	private static void ClearMergeProtection(Pickupable pickupable)
	{
		if (!((Object)(object)pickupable == (Object)null))
		{
			int instanceID = ((Object)((Component)pickupable).gameObject).GetInstanceID();
			s_mergeProtectedUntilTime.Remove(instanceID);
		}
	}

	private static void RemoveMergedInventoryRow(InventoryItem row, ItemsContainer container, Pickupable survivorPickup)
	{
		if (!((Object)(object)((row != null) ? row.item : null) == (Object)null) && container != null)
		{
			Pickupable item = row.item;
			if ((Object)(object)Inventory.main != (Object)null && container == Inventory.main.container)
			{
				StackNotificationFix.RecordMergedRow(item, survivorPickup);
			}
			if (container != null)
			{
				((IItemsContainer)container).RemoveItem(row, true, false);
			}
			else
			{
				container.RemoveItem(item, true);
			}
			if (item != survivorPickup)
			{
				Object.Destroy((Object)(object)((Component)item).gameObject);
			}
		}
	}
}
