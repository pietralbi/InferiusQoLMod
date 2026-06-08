#nullable disable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class LifepodStorageScope
{
	public static bool IsLifepodOrTimeCapsuleStorage(ItemsContainer container)
	{
		if (container == null)
		{
			return false;
		}
		if (container != null)
		{
			string label = ((IItemsContainer)container).label;
			if (!string.IsNullOrEmpty(label) && label.IndexOf("TimeCapsule", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		if ((Object)(object)PlayerTimeCapsule.main != (Object)null && container == PlayerTimeCapsule.main.container)
		{
			return true;
		}
		return false;
	}
}
