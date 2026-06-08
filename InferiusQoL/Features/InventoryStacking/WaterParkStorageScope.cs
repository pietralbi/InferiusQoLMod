#nullable disable
using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class WaterParkStorageScope
{
	private static readonly Type[] HabitatRootTypes = BuildHabitatRootTypes();

	private static Type[] BuildHabitatRootTypes()
	{
		string[] array = new string[5] { "WaterPark", "Aquarium", "WaterParkItem", "LargeRoomWaterPark", "LargeRoomWaterParkPlanter" };
		Type[] array2 = new Type[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = AccessTools.TypeByName(array[i]);
		}
		return array2;
	}

	public static bool IsCreatureHabitatContainer(ItemsContainer container)
	{
		return IsCreatureHabitatContainer((IItemsContainer)(object)container);
	}

	public static bool IsCreatureHabitatContainer(IItemsContainer container)
	{
		if (container == null)
		{
			return false;
		}
		string label = container.label;
		if (!string.IsNullOrEmpty(label) && ContainsHabitatToken(label))
		{
			return true;
		}
		ItemsContainer val = (ItemsContainer)(object)((container is ItemsContainer) ? container : null);
		if ((Object)(object)((val != null) ? val.tr : null) == (Object)null)
		{
			return false;
		}
		Transform val2 = val.tr;
		int num = 0;
		while ((Object)(object)val2 != (Object)null && num < 16)
		{
			if (ContainsHabitatToken(((Object)val2).name))
			{
				return true;
			}
			for (int i = 0; i < HabitatRootTypes.Length; i++)
			{
				Type type = HabitatRootTypes[i];
				if (type != null && (Object)(object)((Component)val2).GetComponent(type) != (Object)null)
				{
					return true;
				}
			}
			Component[] components = ((Component)val2).GetComponents<Component>();
			foreach (Component val3 in components)
			{
				if (!((Object)(object)val3 == (Object)null) && ContainsHabitatToken(((object)val3).GetType().Name))
				{
					return true;
				}
			}
			val2 = val2.parent;
			num++;
		}
		return false;
	}

	private static bool ContainsHabitatToken(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (text.IndexOf("WaterPark", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text.IndexOf("Aquarium", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	public static void EnforceSingleUnitInHabitat(InventoryItem item, ItemsContainer container)
	{
		if ((Object)(object)((item != null) ? item.item : null) == (Object)null || !IsCreatureHabitatContainer(container))
		{
			return;
		}
		int num = MRStack.CountOf(item.item);
		if (num > 1)
		{
			TechType techType = item.item.GetTechType();
			int num2 = num - 1;
			MRStack.Ensure(item.item, 1);
			StackIconRefresher.Trigger();
			if (num2 > 0 && (Object)(object)Player.main != (Object)null)
			{
				((MonoBehaviour)Player.main).StartCoroutine(ReturnExtrasToPlayer(techType, num2));
			}
		}
	}

	private static IEnumerator ReturnExtrasToPlayer(TechType tech, int extraCount)
	{
		if (extraCount <= 0 || (Object)(object)Inventory.main == (Object)null)
		{
			yield break;
		}
		var spawned = new StackedPrefab<Pickupable>();
		yield return StackedPrefabFactory.InstantiatePickup(tech, extraCount, spawned);
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
}
