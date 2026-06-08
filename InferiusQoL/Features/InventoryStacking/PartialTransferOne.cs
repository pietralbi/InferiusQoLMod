#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class PartialTransferOne
{
	private static readonly Type[] ReactorTypes = new Type[4]
	{
		AccessTools.TypeByName("BaseBioReactor"),
		AccessTools.TypeByName("BioReactor"),
		AccessTools.TypeByName("BaseNuclearReactor"),
		AccessTools.TypeByName("NuclearReactor")
	};

	private static readonly Dictionary<IItemsContainer, bool> SingleUnitTargetCache = new Dictionary<IItemsContainer, bool>();

	private static int s_mergeSuppressTicket;

	public static bool TryStart(InventoryItem source)
	{
		return TryStartMove(source, 1);
	}

	public static void TryDropOne(InventoryItem source)
	{
		if (source != null && !((Object)(object)Player.main == (Object)null) && !((Object)(object)Inventory.main == (Object)null))
		{
			Pickupable item = source.item;
			if (!((Object)(object)item == (Object)null) && StackRules.CanStack(item) && MRStack.CountOf(item) > 1)
			{
				((MonoBehaviour)Player.main).StartCoroutine(RunDropOne(source));
			}
		}
	}

	public static bool TryConsumeOneFromStack(InventoryItem source, ItemAction action)
	{
		if ((int)action != 2 && (int)action != 1)
		{
			return false;
		}
		if (source == null || (Object)(object)Player.main == (Object)null || (Object)(object)Inventory.main == (Object)null)
		{
			return false;
		}
		Pickupable item = source.item;
		if ((Object)(object)item == (Object)null || !StackRules.CanStack(item))
		{
			return false;
		}
		if (MRStack.CountOf(item) <= 1)
		{
			return false;
		}
		((MonoBehaviour)Player.main).StartCoroutine(RunConsumeOneFromStack(source, action));
		return true;
	}

	public static bool TrySplitInSameContainer(InventoryItem source, int moveCount)
	{
		if (source == null || (Object)(object)Player.main == (Object)null || (Object)(object)Inventory.main == (Object)null)
		{
			return false;
		}
		if ((object)source.container != Inventory.main.container)
		{
			return false;
		}
		Pickupable item = source.item;
		if ((Object)(object)item == (Object)null || !StackRules.CanStack(item))
		{
			return false;
		}
		int num = MRStack.CountOf(item);
		if (moveCount < 1 || moveCount >= num)
		{
			return false;
		}
		((MonoBehaviour)Player.main).StartCoroutine(RunSplitSameContainer(source, moveCount));
		return true;
	}

	public static void TryStartHalf(InventoryItem source)
	{
		if ((Object)(object)((source != null) ? source.item : null) == (Object)null)
		{
			return;
		}
		Pickupable item = source.item;
		if (!StackRules.CanStack(item))
		{
			return;
		}
		int num = MRStack.CountOf(item);
		if (num > 1)
		{
			int num2 = num / 2;
			if (num2 >= 1)
			{
				TryStartMove(source, num2);
			}
		}
	}

	private static bool TryStartMove(InventoryItem source, int moveCount)
	{
		if (source == null || (Object)(object)Player.main == (Object)null)
		{
			return false;
		}
		Pickupable item = source.item;
		if ((Object)(object)item == (Object)null || !StackRules.CanStack(item))
		{
			return false;
		}
		if (MRStack.CountOf(item) <= moveCount)
		{
			return false;
		}
		IItemsContainer oppositeContainer = Inventory.main.GetOppositeContainer(source);
		if (oppositeContainer == null)
		{
			return false;
		}
		((MonoBehaviour)Player.main).StartCoroutine(Run(source, oppositeContainer, moveCount));
		return true;
	}

	private static InventoryItem FindItemForPickup(ItemsContainer c, Pickupable target)
	{
		if (c == null || (Object)(object)target == (Object)null)
		{
			return null;
		}
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)c)
		{
			if (item != null && (Object)(object)item.item == (Object)(object)target)
			{
				return item;
			}
		}
		return null;
	}

	private static IEnumerator Run(InventoryItem source, IItemsContainer destContainer, int moveCount)
	{
		Pickupable srcP = source.item;
		ItemsContainer mainContainer = Inventory.main.container;
		bool destIsMain = (object)destContainer == mainContainer;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<Pickupable>();
		MRStack.SuppressMerge = true;
		yield return StackedPrefabFactory.InstantiatePickup(tech, moveCount, spawned);
		Pickupable component = spawned.Pickupable;
		if ((Object)(object)component == (Object)null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		if (!Inventory.main.Pickup(component, false))
		{
			Object.Destroy((Object)(object)spawned.GameObject);
			ErrorMessage.AddError(Language.main.Get("InventoryFull"));
			MRStack.SuppressMerge = false;
			yield break;
		}
		InventoryItem movedItem = FindItemForPickup(mainContainer, component);
		if (movedItem == null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		int num = MRStack.CountOf(srcP);
		MRStack.Add(srcP, -moveCount);
		if (num <= moveCount)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		MRStack.SuppressMerge = false;
		if (destIsMain)
		{
			StackConsolidation.AfterUnsafeAdd(movedItem, mainContainer);
		}
		else if (ReactorFeedHelper.IsReactorContainer(destContainer))
		{
			Inventory.main.ExecuteItemAction(movedItem, 0);
			if (movedItem.container != null && (object)movedItem.container == mainContainer)
			{
				MRStack.SuppressMerge = true;
				MRStack.Add(srcP, moveCount);
				mainContainer.RemoveItem(component, true);
				Object.Destroy((Object)(object)((Component)component).gameObject);
				MRStack.SuppressMerge = false;
				StackIconRefresher.Trigger();
				yield break;
			}
		}
		else if (!Inventory.AddOrSwap(movedItem, destContainer))
		{
			MRStack.SuppressMerge = true;
			MRStack.Add(srcP, moveCount);
			mainContainer.RemoveItem(component, true);
			Object.Destroy((Object)(object)((Component)component).gameObject);
			MRStack.SuppressMerge = false;
			StackIconRefresher.Trigger();
			yield break;
		}
		StackIconRefresher.Trigger();
	}

	public static void ClearSingleUnitTargetCache()
	{
		SingleUnitTargetCache.Clear();
	}

	public static bool IsSingleUnitInsertTarget(IItemsContainer container)
	{
		if (container == null)
		{
			return false;
		}
		if ((Object)(object)Inventory.main != (Object)null)
		{
			if ((object)container == Inventory.main.container)
			{
				return false;
			}
			if ((object)container == Inventory.main.equipment)
			{
				return false;
			}
		}
		if (SingleUnitTargetCache.TryGetValue(container, out var value))
		{
			return value;
		}
		bool flag = IsSingleUnitInsertTargetCore(container);
		SingleUnitTargetCache[container] = flag;
		return flag;
	}

	private static bool IsSingleUnitInsertTargetCore(IItemsContainer container)
	{
		if (WaterParkStorageScope.IsCreatureHabitatContainer(container))
		{
			return true;
		}
		if (ReactorFeedHelper.IsSingleUnitContainer(container))
		{
			return true;
		}
		ItemsContainer itemsContainer = (ItemsContainer)(object)((container is ItemsContainer) ? container : null);
		if (itemsContainer != null && IsNuclearRodOnlyContainer(itemsContainer))
		{
			return true;
		}
		if (ContainsReactorToken(container.label))
		{
			return true;
		}
		IItemsContainer obj = ((container is ItemsContainer) ? container : null);
		Transform transform = ((obj != null) ? ((ItemsContainer)obj).tr : null);
		int num = 0;
		while ((Object)(object)transform != (Object)null && num < 12)
		{
			if (ContainsReactorToken(((Object)transform).name))
			{
				return true;
			}
			for (int i = 0; i < ReactorTypes.Length; i++)
			{
				Type type = ReactorTypes[i];
				if (type != null && (Object)(object)((Component)transform).GetComponent(type) != (Object)null)
				{
					return true;
				}
			}
			Component[] components = ((Component)transform).GetComponents<Component>();
			foreach (Component component in components)
			{
				if (!((Object)(object)component == (Object)null))
				{
					Type type2 = ((object)component).GetType();
					string fullName = type2.FullName;
					string name = type2.Name;
					if (ContainsReactorToken(fullName) || ContainsReactorToken(name))
					{
						return true;
					}
				}
			}
			transform = transform.parent;
			num++;
		}
		return false;
	}

	private static bool IsNuclearRodOnlyContainer(ItemsContainer container)
	{
		HashSet<TechType> value = Traverse.Create((object)container).Field<HashSet<TechType>>("allowedTech").Value;
		if (value == null || value.Count != 1)
		{
			return false;
		}
		return value.Contains((TechType)64);
	}

	private static bool ContainsReactorToken(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("reactor", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text.IndexOf("nuclear", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return text.IndexOf("bioreactor", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static IEnumerator RunDropOne(InventoryItem source)
	{
		Pickupable srcP = source.item;
		ItemsContainer mainContainer = Inventory.main.container;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<Pickupable>();
		MRStack.SuppressMerge = true;
		yield return StackedPrefabFactory.InstantiatePickup(tech, 1, spawned);
		Pickupable component = spawned.Pickupable;
		if ((Object)(object)component == (Object)null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		if (!Inventory.main.Pickup(component, false))
		{
			Object.Destroy((Object)(object)spawned.GameObject);
			MRStack.SuppressMerge = false;
			yield break;
		}
		InventoryItem droppedItem = FindItemForPickup(mainContainer, component);
		if (droppedItem == null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		MRStack.Add(srcP, -1);
		MRStack.SuppressMerge = false;
		Inventory.main.ExecuteItemAction((ItemAction)128, droppedItem);
		StackIconRefresher.Trigger();
	}

	private static void DecrementStackAfterSuccessfulConsume(Pickupable stackPickup)
	{
		if (!((Object)(object)stackPickup == (Object)null))
		{
			MRStack.Add(stackPickup, -1);
		}
	}

	private static IEnumerator RunConsumeOneFromStack(InventoryItem source, ItemAction action)
	{
		Pickupable srcP = source.item;
		ItemsContainer mainContainer = Inventory.main.container;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<Pickupable>();
		MRStack.SuppressMerge = true;
		yield return StackedPrefabFactory.InstantiatePickup(tech, 1, spawned);
		Pickupable component = spawned.Pickupable;
		if ((Object)(object)component == (Object)null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		float splitMergeProtectionSeconds = StackConfig.SplitMergeProtectionSeconds;
		StackConsolidation.ProtectFromMerge(srcP, splitMergeProtectionSeconds);
		StackConsolidation.ProtectFromMerge(component, splitMergeProtectionSeconds);
		if (!Inventory.main.Pickup(component, false))
		{
			Object.Destroy((Object)(object)spawned.GameObject);
			MRStack.SuppressMerge = false;
			yield break;
		}
		InventoryItem singletonItem = FindItemForPickup(mainContainer, component);
		if (singletonItem == null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		Pickupable singletonForEatOrUse = singletonItem.item;
		Inventory.main.ExecuteItemAction(action, singletonItem);
		yield return null;
		if ((Object)(object)singletonForEatOrUse != (Object)null)
		{
			MRStack.SuppressMerge = false;
			StackIconRefresher.ForceRefresh();
			yield break;
		}
		DecrementStackAfterSuccessfulConsume(srcP);
		StackIconRefresher.RefreshIconsForPickup(srcP);
		MRStack.SuppressMerge = false;
		yield return null;
		StackIconRefresher.RefreshIconsForPickup(srcP);
		StackIconRefresher.ForceRefresh();
	}

	private static IEnumerator RunSplitSameContainer(InventoryItem source, int moveCount)
	{
		Pickupable srcP = source.item;
		ItemsContainer container = Inventory.main.container;
		if ((Object)(object)srcP == (Object)null || container == null || MRStack.CountOf(srcP) <= moveCount)
		{
			yield break;
		}
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<Pickupable>();
		MRStack.SuppressMerge = true;
		int ticket = ++s_mergeSuppressTicket;
		yield return StackedPrefabFactory.InstantiatePickup(tech, moveCount, spawned);
		Pickupable component = spawned.Pickupable;
		if ((Object)(object)component == (Object)null)
		{
			ReleaseMergeSuppression(ticket);
			yield break;
		}
		if (!Inventory.main.Pickup(component, false))
		{
			Object.Destroy((Object)(object)spawned.GameObject);
			ReleaseMergeSuppression(ticket);
			ErrorMessage.AddError(Language.main.Get("InventoryFull"));
			yield break;
		}
		MRStack.Add(srcP, -moveCount);
		float splitMergeProtectionSeconds = StackConfig.SplitMergeProtectionSeconds;
		StackConsolidation.ProtectFromMerge(srcP, splitMergeProtectionSeconds);
		StackConsolidation.ProtectFromMerge(component, splitMergeProtectionSeconds);
		((MonoBehaviour)Player.main).StartCoroutine(ReleaseMergeSuppressionDelayed(ticket));
		StackIconRefresher.Trigger();
	}

	private static IEnumerator ReleaseMergeSuppressionDelayed(int ticket)
	{
		yield return null;
		yield return null;
		ReleaseMergeSuppression(ticket);
	}

	private static void ReleaseMergeSuppression(int ticket)
	{
		if (ticket == s_mergeSuppressTicket)
		{
			MRStack.SuppressMerge = false;
		}
	}
}
