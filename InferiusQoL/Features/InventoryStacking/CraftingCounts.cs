#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class CraftingCounts
{
	private readonly struct CacheKey(ItemsContainer container, TechType tech)
	{
		private readonly ItemsContainer _container = container;

		private readonly TechType _tech = tech;
	}

	[ThreadStatic]
	private static int _craftingQueryDepth;

	private static int _cacheFrame = -1;

	private static readonly Dictionary<CacheKey, int> _cache = new Dictionary<CacheKey, int>();

	public static bool InCraftingQuery => _craftingQueryDepth > 0;

	public static void EnterCraftingQuery()
	{
		_craftingQueryDepth++;
	}

	public static void ExitCraftingQuery()
	{
		if (_craftingQueryDepth > 0)
		{
			_craftingQueryDepth--;
		}
	}

	public static int PickupUnitsForCraft(Inventory inventory, TechType tech)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)inventory == (Object)null)
		{
			return CraftFromContainersCompat.NearbyUnits(tech);
		}
		int num = 0;
		if (inventory.container != null)
		{
			num += MRStack.TotalStackUnits(inventory.container, tech);
		}
		int usedStorageCount = inventory.GetUsedStorageCount();
		for (int i = 0; i < usedStorageCount; i++)
		{
			IItemsContainer usedStorage = inventory.GetUsedStorage(i);
			ItemsContainer val = (ItemsContainer)(object)((usedStorage is ItemsContainer) ? usedStorage : null);
			if (val != null)
			{
				num += MRStack.TotalStackUnits(val, tech);
			}
		}
		return num + CraftFromContainersCompat.NearbyUnits(tech);
	}

	public static int UnitsInContainer(Inventory inventory, TechType tech)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		ItemsContainer val = ((inventory != null) ? inventory.container : null);
		if (val == null)
		{
			return 0;
		}
		return UnitsIn(val, tech);
	}

	public static int UnitsIn(ItemsContainer container, TechType tech)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (container == null)
		{
			return 0;
		}
		EnsureCacheFrame();
		CacheKey key = new CacheKey(container, tech);
		if (_cache.TryGetValue(key, out var value))
		{
			return value;
		}
		int num = MRStack.TotalStackUnits(container, tech);
		_cache[key] = num;
		return num;
	}

	private static void EnsureCacheFrame()
	{
		int frameCount = Time.frameCount;
		if (frameCount != _cacheFrame)
		{
			_cacheFrame = frameCount;
			_cache.Clear();
		}
	}

	internal static void InvalidateCache()
	{
		_cacheFrame = -1;
		_cache.Clear();
	}
}
