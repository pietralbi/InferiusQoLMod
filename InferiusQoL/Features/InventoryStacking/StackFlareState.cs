#nullable disable
using System.Collections.Generic;
using InferiusQoL.Localization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackFlareState
{
	private const string UsedFlareNameKey = "InferiusQoL.UsedFlare";

	public static readonly TechType FlareTechType = (TechType)754;

	public static string UsedFlareName => L.GetOrFallback(UsedFlareNameKey, "Used Flare");

	public static bool IsFlareTech(TechType techType)
	{
		return (int)techType == (int)FlareTechType;
	}

	public static bool IsFlare(Pickupable pickupable)
	{
		return (Object)(object)pickupable != (Object)null && IsFlareTech(pickupable.GetTechType());
	}

	public static bool IsUsedFlare(Pickupable pickupable)
	{
		Flare flare = GetFlare(pickupable);
		return (Object)(object)flare != (Object)null && (flare.hasBeenThrown || flare.flareActiveState);
	}

	public static bool IsUnusedFlare(Pickupable pickupable)
	{
		if (!IsFlare(pickupable))
		{
			return false;
		}

		Flare flare = GetFlare(pickupable);
		return (Object)(object)flare != (Object)null && !flare.hasBeenThrown && !flare.flareActiveState;
	}

	public static bool CanMerge(Pickupable a, Pickupable b)
	{
		bool flareA = IsFlare(a);
		bool flareB = IsFlare(b);
		if (!flareA && !flareB)
		{
			return true;
		}

		return flareA && flareB && IsUnusedFlare(a) && IsUnusedFlare(b);
	}

	public static InventoryItem FindUsedInventoryFlare(QuickSlots quickSlots, bool requireUnbound)
	{
		ItemsContainer container = quickSlots != null ? quickSlots.container : Inventory.main?.container;
		if (container == null)
		{
			return null;
		}

		IList<InventoryItem> items = container.GetItems(FlareTechType);
		if (items == null)
		{
			return null;
		}

		for (int i = 0; i < items.Count; i++)
		{
			InventoryItem item = items[i];
			if ((Object)(object)((item != null) ? item.item : null) == (Object)null)
			{
				continue;
			}
			if (!IsUsedFlare(item.item))
			{
				continue;
			}
			if (requireUnbound && quickSlots != null && quickSlots.GetSlotByItem(item) != -1)
			{
				continue;
			}

			return item;
		}

		return null;
	}

	public static void WriteUsedFlareTooltip(TooltipData data, Pickupable pickupable, InventoryItem item = null, bool includeActions = false)
	{
		if (data == null || (Object)(object)pickupable == (Object)null)
		{
			return;
		}

		TooltipFactory.Initialize();
		TooltipFactory.WriteTitle(data.prefix, UsedFlareName);
		TooltipFactory.WriteDebug(data.prefix, FlareTechType);
		TooltipFactory.WriteDescription(data.prefix, Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(FlareTechType)));
		if (includeActions && item != null)
		{
			TooltipFactory.ItemActions(data.postfix, item);
		}
	}

	private static Flare GetFlare(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null || !IsFlare(pickupable))
		{
			return null;
		}

		var component = (Component)pickupable;
		Flare flare = component.GetComponent<Flare>();
		return (Object)(object)flare != (Object)null ? flare : component.GetComponentInChildren<Flare>(true);
	}
}
