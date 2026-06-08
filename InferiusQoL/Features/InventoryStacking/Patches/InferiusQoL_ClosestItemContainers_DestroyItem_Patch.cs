#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class InferiusQoL_ClosestItemContainers_DestroyItem_Patch
{
	public static bool Prepare()
	{
		Type closestContainersType = InferiusQoLCompat.ClosestContainersType;
		if (closestContainersType == null)
		{
			return false;
		}
		return AccessTools.Method(closestContainersType, "DestroyItem", new Type[2]
		{
			typeof(TechType),
			typeof(int)
		}, (Type[])null) != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase Target()
	{
		return AccessTools.Method(InferiusQoLCompat.ClosestContainersType, "DestroyItem", new Type[2]
		{
			typeof(TechType),
			typeof(int)
		}, (Type[])null);
	}

	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool Prefix(TechType techType, int count, ref bool __result)
	{
		ItemsContainer[] containers = InferiusQoLCompat.GetContainers();
		if (containers == null || count <= 0)
		{
			__result = count <= 0;
			return false;
		}
		int num = count;
		for (int i = 0; i < containers.Length; i++)
		{
			if (num <= 0)
			{
				break;
			}
			ItemsContainer val = containers[i];
			if (val != null)
			{
				num -= ConsumeStackUnits(val, techType, num);
			}
		}
		__result = num == 0;
		return false;
	}

	private static int ConsumeStackUnits(ItemsContainer container, TechType tech, int count)
	{
		IList<InventoryItem> items = container.GetItems(tech);
		if (items == null || items.Count == 0)
		{
			return 0;
		}
		int num = 0;
		List<InventoryItem> list = new List<InventoryItem>(items);
		for (int i = 0; i < list.Count; i++)
		{
			if (num >= count)
			{
				break;
			}
			InventoryItem val = list[i];
			Pickupable val2 = ((val != null) ? val.item : null);
			if ((Object)(object)val2 == (Object)null)
			{
				continue;
			}
			int num2 = ((!StackRules.CanStack(val2)) ? 1 : MRStack.CountOf(val2));
			int num3 = count - num;
			if (num2 <= num3)
			{
				if (RemoveAndDestroyRow(container, val, val2))
				{
					num += num2;
				}
			}
			else
			{
				MRStack.SetAmount(val2, num2 - num3);
				num += num3;
				StackIconRefresher.Trigger();
			}
		}
		return num;
	}

	private static bool RemoveAndDestroyRow(ItemsContainer container, InventoryItem row, Pickupable p)
	{
		if (container != null)
		{
			if (!((IItemsContainer)container).RemoveItem(row, true, false))
			{
				return false;
			}
		}
		else if (!container.RemoveItem(p, true))
		{
			return false;
		}
		Object.Destroy((Object)(object)((Component)p).gameObject);
		return true;
	}
}
