#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackCapEnforcer
{
	public static void EnforceCapOnAllPlayerInventories()
	{
		if (!((Object)(object)Inventory.main == (Object)null) && !((Object)(object)Plugin.Instance == (Object)null))
		{
			((MonoBehaviour)Plugin.Instance).StartCoroutine(CoEnforcePlayerInventories());
		}
	}

	private static IEnumerator CoEnforcePlayerInventories()
	{
		yield return null;
		Inventory inv = Inventory.main;
		if ((Object)(object)inv == (Object)null)
		{
			yield break;
		}
		yield return CoEnforceCapOnContainer(inv.container);
		int n = inv.GetUsedStorageCount();
		for (int i = 0; i < n; i++)
		{
			IItemsContainer usedStorage = inv.GetUsedStorage(i);
			ItemsContainer val = (ItemsContainer)(object)((usedStorage is ItemsContainer) ? usedStorage : null);
			if (val != null)
			{
				yield return CoEnforceCapOnContainer(val);
			}
		}
		StackIconRefresher.Trigger();
	}

	private static IEnumerator CoEnforceCapOnContainer(ItemsContainer container)
	{
		if (container == null)
		{
			yield break;
		}
		int cap = StackConfig.MaxStackSize;
		List<Pickupable> snapshot = new List<Pickupable>();
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			Pickupable val = ((item != null) ? item.item : null);
			if ((Object)(object)val != (Object)null && StackRules.CanStack(val))
			{
				snapshot.Add(val);
			}
		}
		for (int i = 0; i < snapshot.Count; i++)
		{
			yield return CoSplitDownToCap(snapshot[i], container, cap);
		}
	}

	private static IEnumerator CoSplitDownToCap(Pickupable source, ItemsContainer container, int cap)
	{
		if ((Object)(object)source == (Object)null || container == null || cap < 1)
		{
			yield break;
		}
		int guard = 0;
		while (Stack.CountOf(source) > cap && guard++ < 256)
		{
			int num = Stack.CountOf(source);
			int num2 = Mathf.Min(cap, num - cap);
			if (num2 < 1)
			{
				break;
			}
			bool splitOk = false;
			yield return CoTrySplitOff(container, source, num2, delegate(bool ok)
			{
				splitOk = ok;
			});
			if (!splitOk)
			{
				break;
			}
		}
	}

	private static IEnumerator CoTrySplitOff(ItemsContainer container, Pickupable source, int moveCount, Action<bool> done)
	{
		done(obj: false);
		if (container == null || (Object)(object)source == (Object)null || moveCount < 1)
		{
			yield break;
		}
		int have = Stack.CountOf(source);
		if (moveCount >= have)
		{
			yield break;
		}
		TechType tech = source.GetTechType();
		var spawned = new StackedPrefab<Pickupable>();
		yield return StackedPrefabFactory.InstantiatePickup(tech, moveCount, spawned, source);
		Pickupable component = spawned.Pickupable;
		if ((Object)(object)component == (Object)null)
		{
			yield break;
		}
		if (container.AddItem(component) == null)
		{
			Object.Destroy((Object)(object)spawned.GameObject);
			yield break;
		}
		Stack.SetAmount(source, have - moveCount);
		done(obj: true);
	}
}
