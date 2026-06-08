#nullable disable
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackIconRefresher
{
	internal static uGUI_QuickSlots CachedQuickSlotsUi;

	private static int s_lastTriggerFrame = -1;

	public static void Trigger()
	{
		int frameCount = Time.frameCount;
		if (s_lastTriggerFrame != frameCount)
		{
			s_lastTriggerFrame = frameCount;
			RefreshAllStackLabels();
		}
	}

	public static void ForceRefresh()
	{
		s_lastTriggerFrame = Time.frameCount;
		RefreshAllStackLabels();
	}

	public static void RefreshIconsForPickup(Pickupable p)
	{
		if ((Object)(object)p == (Object)null)
		{
			return;
		}
		ItemsContainerViewRegistry.ForEachActive(delegate(uGUI_ItemsContainer view)
		{
			if (!((Object)(object)view == (Object)null))
			{
				ItemsContainer value3 = Traverse.Create((object)view).Field<ItemsContainer>("container").Value;
				if (value3 != null)
				{
					foreach (InventoryItem item in (IEnumerable<InventoryItem>)value3)
					{
						if (!((Object)(object)((item != null) ? item.item : null) == (Object)null) && item.item == p)
						{
							uGUI_ItemIcon icon = view.GetIcon(item);
							if ((Object)(object)icon != (Object)null)
							{
								StackIconHelper.UpdateForPickup(icon, p);
							}
						}
					}
				}
			}
		});
		uGUI_QuickSlots cachedQuickSlotsUi = CachedQuickSlotsUi;
		if ((Object)(object)cachedQuickSlotsUi == (Object)null)
		{
			return;
		}
		uGUI_ItemIcon[] value = Traverse.Create((object)cachedQuickSlotsUi).Field<uGUI_ItemIcon[]>("icons").Value;
		object value2 = Traverse.Create((object)cachedQuickSlotsUi).Method("GetTarget", Array.Empty<object>()).GetValue();
		IQuickSlots val = (IQuickSlots)((value2 is IQuickSlots) ? value2 : null);
		if (value == null || val == null)
		{
			return;
		}
		for (int num = 0; num < value.Length; num++)
		{
			uGUI_ItemIcon val2 = value[num];
			if (!((Object)(object)val2 == (Object)null))
			{
				InventoryItem slotItem = val.GetSlotItem(num);
				if ((Object)(object)((slotItem != null) ? slotItem.item : null) != (Object)null && slotItem.item == p)
				{
					StackIconHelper.UpdateForPickup(val2, p);
				}
			}
		}
	}

	public static void RefreshAllStackLabels()
	{
		ItemsContainerViewRegistry.ForEachActive(delegate(uGUI_ItemsContainer view)
		{
			if (!((Object)(object)view == (Object)null))
			{
				ItemsContainer value = Traverse.Create((object)view).Field<ItemsContainer>("container").Value;
				if (value != null)
				{
					foreach (InventoryItem item in (IEnumerable<InventoryItem>)value)
					{
						if (!((Object)(object)((item != null) ? item.item : null) == (Object)null))
						{
							uGUI_ItemIcon icon = view.GetIcon(item);
							if ((Object)(object)icon != (Object)null)
							{
								StackIconHelper.UpdateForPickup(icon, item.item);
							}
						}
					}
				}
			}
		});
		RefreshQuickSlotsCached();
	}

	private static void RefreshQuickSlotsCached()
	{
		uGUI_QuickSlots cachedQuickSlotsUi = CachedQuickSlotsUi;
		if ((Object)(object)cachedQuickSlotsUi == (Object)null)
		{
			return;
		}
		uGUI_ItemIcon[] value = Traverse.Create((object)cachedQuickSlotsUi).Field<uGUI_ItemIcon[]>("icons").Value;
		object value2 = Traverse.Create((object)cachedQuickSlotsUi).Method("GetTarget", Array.Empty<object>()).GetValue();
		IQuickSlots val = (IQuickSlots)((value2 is IQuickSlots) ? value2 : null);
		if (value == null || val == null)
		{
			return;
		}
		for (int i = 0; i < value.Length; i++)
		{
			uGUI_ItemIcon val2 = value[i];
			if (!((Object)(object)val2 == (Object)null))
			{
				InventoryItem slotItem = val.GetSlotItem(i);
				if ((Object)(object)((slotItem != null) ? slotItem.item : null) != (Object)null)
				{
					StackIconHelper.UpdateForPickup(val2, slotItem.item);
				}
			}
		}
	}
}
