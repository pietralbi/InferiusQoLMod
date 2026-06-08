#nullable disable
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

internal static class InventorySwapPatchHelper
{
	internal static void TryMergeMovedItem(bool ok, InventoryItem itemA)
	{
		if (ok && !((Object)(object)((itemA != null) ? itemA.item : null) == (Object)null))
		{
			IItemsContainer container = itemA.container;
			ItemsContainer val = (ItemsContainer)(object)((container is ItemsContainer) ? container : null);
			if (val != null)
			{
				StackConsolidation.TryMerge(itemA, val);
			}
		}
	}
}
