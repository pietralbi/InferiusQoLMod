#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class ItemsContainerViewRegistry
{
	private static readonly object s_lock = new object();

	private static readonly HashSet<uGUI_ItemsContainer> s_views = new HashSet<uGUI_ItemsContainer>();

	public static void Register(uGUI_ItemsContainer view)
	{
		if ((Object)(object)view == (Object)null)
		{
			return;
		}
		lock (s_lock)
		{
			s_views.Add(view);
		}
		StackIconRefresher.MarkViewDirty(view);
	}

	public static void Unregister(uGUI_ItemsContainer view)
	{
		if ((Object)(object)view == (Object)null)
		{
			return;
		}
		lock (s_lock)
		{
			s_views.Remove(view);
		}
	}

	public static void ForEachActive(Action<uGUI_ItemsContainer> action)
	{
		if (action == null)
		{
			return;
		}
		List<uGUI_ItemsContainer> list;
		lock (s_lock)
		{
			list = new List<uGUI_ItemsContainer>(s_views);
		}
		for (int i = 0; i < list.Count; i++)
		{
			uGUI_ItemsContainer val = list[i];
			if ((Object)(object)val != (Object)null && ((Component)val).gameObject.activeInHierarchy)
			{
				action(val);
			}
		}
	}
}
