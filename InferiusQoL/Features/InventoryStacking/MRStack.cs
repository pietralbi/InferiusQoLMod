#nullable disable
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class MRStack
{
	public static volatile bool SuppressMerge;

	public static int CountOf(Pickupable p)
	{
		if ((Object)(object)p == (Object)null)
		{
			return 0;
		}
		MRStackData[] components = ((Component)p).gameObject.GetComponents<MRStackData>();
		if (components == null || components.Length == 0)
		{
			return 1;
		}
		if (components.Length > 1)
		{
			MRStackData mRStackData = components[0];
			if ((Object)(object)mRStackData != (Object)null && mRStackData.amount >= 1)
			{
				return mRStackData.amount;
			}
		}
		if (components.Length == 1)
		{
			if (!((Object)(object)components[0] == (Object)null) && components[0].amount >= 1)
			{
				return components[0].amount;
			}
			return 1;
		}
		int num = 1;
		foreach (MRStackData mRStackData2 in components)
		{
			if (!((Object)(object)mRStackData2 == (Object)null))
			{
				int amount = mRStackData2.amount;
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
		MRStackData[] components = ((Component)p).gameObject.GetComponents<MRStackData>();
		MRStackData mRStackData = null;
		if (components != null && components.Length != 0)
		{
			mRStackData = components[0];
			if (components.Length > 1)
			{
				for (int i = 1; i < components.Length; i++)
				{
					if ((Object)(object)components[i] != (Object)null)
					{
						Object.Destroy((Object)(object)components[i]);
					}
				}
			}
		}
		if ((Object)(object)mRStackData == (Object)null)
		{
			mRStackData = ((Component)p).gameObject.GetComponent<MRStackData>();
		}
		if ((Object)(object)mRStackData == (Object)null)
		{
			mRStackData = ((Component)p).gameObject.AddComponent<MRStackData>();
		}
		mRStackData.amount = num;
	}

	public static void Ensure(Pickupable p, int count, bool clampToConfigMax = true)
	{
		SetAmount(p, count, clampToConfigMax);
	}

	public static void Add(Pickupable p, int delta)
	{
		if (!((Object)(object)p == (Object)null))
		{
			MRStackData[] components = ((Component)p).gameObject.GetComponents<MRStackData>();
			int num = 1;
			num = ((components == null || components.Length == 0 || !((Object)(object)components[0] != (Object)null) || components[0].amount < 1) ? CountOf(p) : components[0].amount);
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

	public static int TotalStackUnits(ItemsContainer container, TechType tech)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (container == null || (Object)(object)pickupable == (Object)null || !StackRules.CanStack(pickupable))
		{
			return false;
		}
		TechType techType = pickupable.GetTechType();
		int maxStackSize = StackConfig.MaxStackSize;
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			if (!((Object)(object)((item != null) ? item.item : null) == (Object)null) && item.item.GetTechType() == techType && StackRules.CanStack(item.item) && CountOf(item.item) < maxStackSize)
			{
				return true;
			}
		}
		return false;
	}
}
