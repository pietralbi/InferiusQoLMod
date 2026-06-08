#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackIconRefresher
{
	private const int PassiveRefreshIntervalFrames = 4;

	internal static uGUI_QuickSlots CachedQuickSlotsUi;

	private static readonly Dictionary<int, int> s_lastViewPassiveRefreshFrame = new Dictionary<int, int>();

	private static int s_lastTriggerFrame = -1;

	private static int s_lastQuickSlotsPassiveRefreshFrame = -1;

	public static void Trigger()
	{
		int frameCount = Time.frameCount;
		if (s_lastTriggerFrame != frameCount)
		{
			s_lastTriggerFrame = frameCount;
			RefreshAllStackLabels(force: false);
		}
	}

	public static void ForceRefresh()
	{
		s_lastTriggerFrame = Time.frameCount;
		RefreshAllStackLabels(force: true);
	}

	public static void MarkViewDirty(uGUI_ItemsContainer view)
	{
		int viewId = GetObjectId(view);
		if (viewId != 0)
		{
			s_lastViewPassiveRefreshFrame.Remove(viewId);
		}
	}

	public static void RefreshViewFromDoUpdate(uGUI_ItemsContainer view)
	{
		int viewId = GetObjectId(view);
		if (viewId == 0)
		{
			return;
		}

		int frameCount = Time.frameCount;
		if (s_lastViewPassiveRefreshFrame.TryGetValue(viewId, out var lastFrame)
			&& frameCount - lastFrame < PassiveRefreshIntervalFrames)
		{
			return;
		}

		s_lastViewPassiveRefreshFrame[viewId] = frameCount;
		RefreshView(view, force: false);
	}

	public static void RefreshQuickSlotsFromUpdate(uGUI_QuickSlots quickSlots)
	{
		if ((Object)(object)quickSlots == (Object)null)
		{
			return;
		}

		CachedQuickSlotsUi = quickSlots;
		RefreshQuickSlotsCached(force: false, passive: true);
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
				ItemsContainer value3 = StackUiAccess.GetContainer(view);
				if (value3 != null)
				{
					foreach (InventoryItem item in (IEnumerable<InventoryItem>)value3)
					{
						if (!((Object)(object)((item != null) ? item.item : null) == (Object)null) && item.item == p)
						{
							uGUI_ItemIcon icon = view.GetIcon(item);
							if ((Object)(object)icon != (Object)null)
							{
								StackIconHelper.UpdateForPickup(icon, p, force: true);
							}
						}
					}
				}
			}
		});
		RefreshQuickSlotsForPickup(p);
	}

	public static void RefreshAllStackLabels(bool force = false)
	{
		ItemsContainerViewRegistry.ForEachActive(delegate(uGUI_ItemsContainer view)
		{
			RefreshView(view, force);
		});
		RefreshQuickSlotsCached(force, passive: false);
	}

	private static void RefreshView(uGUI_ItemsContainer view, bool force)
	{
		if ((Object)(object)view == (Object)null)
		{
			return;
		}

		ItemsContainer container = StackUiAccess.GetContainer(view);
		if (container == null)
		{
			return;
		}

		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			if (!((Object)(object)((item != null) ? item.item : null) == (Object)null))
			{
				uGUI_ItemIcon icon = view.GetIcon(item);
				if ((Object)(object)icon != (Object)null)
				{
					StackIconHelper.UpdateForPickup(icon, item.item, force);
				}
			}
		}
	}

	private static void RefreshQuickSlotsForPickup(Pickupable p)
	{
		uGUI_QuickSlots cachedQuickSlotsUi = CachedQuickSlotsUi;
		if ((Object)(object)cachedQuickSlotsUi == (Object)null)
		{
			return;
		}

		uGUI_ItemIcon[] icons = StackUiAccess.GetQuickSlotIcons(cachedQuickSlotsUi);
		IQuickSlots target = StackUiAccess.GetQuickSlotsTarget(cachedQuickSlotsUi);
		if (icons == null || target == null)
		{
			return;
		}

		for (int i = 0; i < icons.Length; i++)
		{
			uGUI_ItemIcon icon = icons[i];
			if (!((Object)(object)icon == (Object)null))
			{
				InventoryItem slotItem = target.GetSlotItem(i);
				if ((Object)(object)((slotItem != null) ? slotItem.item : null) != (Object)null && slotItem.item == p)
				{
					StackIconHelper.UpdateForPickup(icon, p, force: true);
				}
			}
		}
	}

	private static void RefreshQuickSlotsCached(bool force, bool passive)
	{
		uGUI_QuickSlots cachedQuickSlotsUi = CachedQuickSlotsUi;
		if ((Object)(object)cachedQuickSlotsUi == (Object)null)
		{
			return;
		}

		if (passive)
		{
			int frameCount = Time.frameCount;
			if (s_lastTriggerFrame == frameCount)
			{
				return;
			}
			if (s_lastQuickSlotsPassiveRefreshFrame >= 0
				&& frameCount - s_lastQuickSlotsPassiveRefreshFrame < PassiveRefreshIntervalFrames)
			{
				return;
			}
			s_lastQuickSlotsPassiveRefreshFrame = frameCount;
		}

		uGUI_ItemIcon[] value = StackUiAccess.GetQuickSlotIcons(cachedQuickSlotsUi);
		IQuickSlots val = StackUiAccess.GetQuickSlotsTarget(cachedQuickSlotsUi);
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
					StackIconHelper.UpdateForPickup(val2, slotItem.item, force);
				}
			}
		}
	}

	private static int GetObjectId(Component component)
	{
		return ((Object)(object)component == (Object)null)
			? 0
			: ((Object)((Component)component).gameObject).GetInstanceID();
	}
}
