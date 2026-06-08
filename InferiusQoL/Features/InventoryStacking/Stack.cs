#nullable disable
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class Stack
{
	public static volatile bool SuppressMerge;

	private static readonly List<StackData> s_stackDataScratch = new List<StackData>(2);

	public static int CountOf(Pickupable p)
	{
		if ((Object)(object)p == (Object)null)
		{
			return 0;
		}

		StackData data = ((Component)p).gameObject.GetComponent<StackData>();
		if ((Object)(object)data == (Object)null)
		{
			return 1;
		}

		if (data.amount >= 1)
		{
			return data.amount;
		}

		return CountOfFallback(p);
	}

	private static int CountOfFallback(Pickupable p)
	{
		s_stackDataScratch.Clear();
		((Component)p).gameObject.GetComponents(s_stackDataScratch);
		if (s_stackDataScratch.Count == 0)
		{
			return 1;
		}

		int num = 1;
		for (int i = 0; i < s_stackDataScratch.Count; i++)
		{
			StackData data = s_stackDataScratch[i];
			if (!((Object)(object)data == (Object)null))
			{
				int amount = data.amount;
				if (amount >= 1)
				{
					num = Mathf.Max(num, amount);
				}
			}
		}
		return num;
	}

	public static void SetAmount(Pickupable p, int count, bool clampToConfigMax = false)
	{
		if ((Object)(object)p == (Object)null)
		{
			return;
		}
		int num = Mathf.Max(1, count);
		if (clampToConfigMax)
		{
			num = Mathf.Min(num, StackConfig.MaxStackSize);
		}
		GameObject gameObject = ((Component)p).gameObject;
		StackData data = gameObject.GetComponent<StackData>();
		if ((Object)(object)data == (Object)null)
		{
			data = gameObject.AddComponent<StackData>();
		}
		data.amount = num;
		RemoveDuplicateData(gameObject, data);
	}

	public static void Ensure(Pickupable p, int count, bool clampToConfigMax = true)
	{
		SetAmount(p, count, clampToConfigMax);
	}

	public static void Add(Pickupable p, int delta)
	{
		if (!((Object)(object)p == (Object)null))
		{
			int num = CountOf(p);
			int num2 = num + delta;
			if (num2 < 1)
			{
				num2 = 1;
			}
			else if (delta > 0)
			{
				num2 = Mathf.Min(num2, StackConfig.MaxStackSize);
			}
			SetAmount(p, num2);
		}
	}

	private static void RemoveDuplicateData(GameObject gameObject, StackData keep)
	{
		s_stackDataScratch.Clear();
		gameObject.GetComponents(s_stackDataScratch);
		if (s_stackDataScratch.Count <= 1)
		{
			return;
		}

		for (int i = 0; i < s_stackDataScratch.Count; i++)
		{
			StackData data = s_stackDataScratch[i];
			if (!((Object)(object)data == (Object)null) && data != keep)
			{
				Object.Destroy((Object)(object)data);
			}
		}
	}

	public static int TotalStackUnits(ItemsContainer container, TechType tech)
	{
		if (container == null)
		{
			return 0;
		}
		IList<InventoryItem> items = container.GetItems(tech);
		if (items == null)
		{
			return 0;
		}
		int num = 0;
		int count = items.Count;
		for (int i = 0; i < count; i++)
		{
			InventoryItem val = items[i];
			if (!((Object)(object)((val != null) ? val.item : null) == (Object)null))
			{
				num += ((!StackRules.CanStack(val.item)) ? 1 : CountOf(val.item));
			}
		}
		return num;
	}

	public static bool ContainerHasMergeRoomFor(ItemsContainer container, Pickupable pickupable)
	{
		if (container == null || (Object)(object)pickupable == (Object)null || !StackRules.CanStack(pickupable))
		{
			return false;
		}
		TechType techType = pickupable.GetTechType();
		int maxStackSize = StackConfig.MaxStackSize;
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			if (!((Object)(object)((item != null) ? item.item : null) == (Object)null)
				&& item.item.GetTechType() == techType
				&& StackRules.CanStack(item.item)
				&& CountOf(item.item) < maxStackSize
				&& StackQuality.CanMerge(item.item, pickupable))
			{
				return true;
			}
		}
		return false;
	}
}
