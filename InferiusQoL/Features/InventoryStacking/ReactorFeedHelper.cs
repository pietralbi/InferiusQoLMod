#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class ReactorFeedHelper
{
	private static bool s_isReturningExtras;

	private static readonly Type PlantableType = AccessTools.TypeByName("Plantable");

	public static void NormalizeInsertedStackToOne(InventoryItem item, ItemsContainer container)
	{
		if ((Object)(object)((item != null) ? item.item : null) == (Object)null || container == null || s_isReturningExtras || !StackRules.CanStack(item.item))
		{
			return;
		}
		Pickupable item2 = item.item;
		TechType techType = item2.GetTechType();
		if (((int)techType != 64 && !IsLikelyPlantableItem(item2)) || !IsSingleUnitContainer((IItemsContainer)(object)container))
		{
			return;
		}
		int num = Stack.CountOf(item.item);
		if (num > 1)
		{
			int num2 = num - 1;
			Stack.Ensure(item.item, 1);
			StackIconRefresher.Trigger();
			if (num2 > 0 && (Object)(object)Player.main != (Object)null)
			{
				((MonoBehaviour)Player.main).StartCoroutine(ReturnExtrasToPlayer(techType, num2, item2));
			}
		}
	}

	public static bool IsLikelyPlantableItem(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null)
		{
			return false;
		}
		if (PlantableType != null && (Object)(object)((Component)pickupable).GetComponent(PlantableType) != (Object)null)
		{
			return true;
		}
		string text = pickupable.GetTechType().ToString();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("seed", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("spore", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text.IndexOf("cutting", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static IEnumerator ReturnExtrasToPlayer(TechType tech, int extraCount, Pickupable source)
	{
		if (extraCount <= 0 || (Object)(object)Inventory.main == (Object)null)
		{
			yield break;
		}
		var spawned = new StackedPrefab<Pickupable>();
		s_isReturningExtras = true;
		yield return StackedPrefabFactory.InstantiatePickup(tech, extraCount, spawned, source);
		s_isReturningExtras = false;
		Pickupable component = spawned.Pickupable;
		if ((Object)(object)component == (Object)null)
		{
			yield break;
		}
		if (!Inventory.main.Pickup(component, false))
		{
			Vector3 dropPosition = ((Component)Player.main).transform.position + ((Component)Player.main).transform.forward * 1.2f;
			component.Drop(dropPosition, default(Vector3), true);
		}
		StackIconRefresher.Trigger();
	}

	public static bool IsSingleUnitContainer(IItemsContainer container)
	{
		ItemsContainer val = (ItemsContainer)(object)((container is ItemsContainer) ? container : null);
		if (val == null)
		{
			return false;
		}
		HashSet<TechType> value = Traverse.Create((object)val).Field<HashSet<TechType>>("allowedTech").Value;
		if (value != null)
		{
			if (value.Contains((TechType)64))
			{
				return true;
			}
			if (value.Count > 0 && value.Count <= 6)
			{
				return true;
			}
		}
		string label = container.label;
		if (ContainsReactorToken(label) || ContainsGrowbedToken(label))
		{
			return true;
		}
		Transform val2 = val.tr;
		int num = 0;
		while ((Object)(object)val2 != (Object)null && num < 12)
		{
			if (ContainsReactorToken(((Object)val2).name) || ContainsGrowbedToken(((Object)val2).name))
			{
				return true;
			}
			Component[] components = ((Component)val2).GetComponents<Component>();
			foreach (Component val3 in components)
			{
				if (!((Object)(object)val3 == (Object)null))
				{
					Type type = ((object)val3).GetType();
					if (ContainsReactorToken(type.Name) || ContainsReactorToken(type.FullName) || ContainsGrowbedToken(type.Name) || ContainsGrowbedToken(type.FullName))
					{
						return true;
					}
				}
			}
			val2 = val2.parent;
			num++;
		}
		return false;
	}

	public static bool IsReactorContainer(IItemsContainer container)
	{
		ItemsContainer val = (ItemsContainer)(object)((container is ItemsContainer) ? container : null);
		if (val == null)
		{
			return false;
		}
		HashSet<TechType> value = Traverse.Create((object)val).Field<HashSet<TechType>>("allowedTech").Value;
		if (value != null && value.Contains((TechType)64))
		{
			return true;
		}
		if (ContainsReactorToken(container.label))
		{
			return true;
		}
		Transform val2 = val.tr;
		int num = 0;
		while ((Object)(object)val2 != (Object)null && num < 12)
		{
			if (ContainsReactorToken(((Object)val2).name))
			{
				return true;
			}
			Component[] components = ((Component)val2).GetComponents<Component>();
			foreach (Component val3 in components)
			{
				if (!((Object)(object)val3 == (Object)null))
				{
					Type type = ((object)val3).GetType();
					if (ContainsReactorToken(type.Name) || ContainsReactorToken(type.FullName))
					{
						return true;
					}
				}
			}
			val2 = val2.parent;
			num++;
		}
		return false;
	}

	private static bool ContainsReactorToken(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("reactor", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("nuclear", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text.IndexOf("bio", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static bool ContainsGrowbedToken(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("growbed", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("planter", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text.IndexOf("farming", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}
}
