#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class StackRules
{
	private const int MaxCanStackCacheEntries = 4096;

	private static readonly Dictionary<int, CanStackCacheEntry> s_canStackCache = new Dictionary<int, CanStackCacheEntry>();

	private struct CanStackCacheEntry
	{
		public int ConfigSignature;

		public bool Result;
	}

	public static bool CanStack(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null)
			return false;

		int configSignature = GetConfigSignature();
		int instanceId = ((Object)((Component)pickupable).gameObject).GetInstanceID();
		if (s_canStackCache.TryGetValue(instanceId, out var entry)
			&& entry.ConfigSignature == configSignature)
		{
			return entry.Result;
		}

		bool result = CanStackUncached(pickupable);
		if (s_canStackCache.Count > MaxCanStackCacheEntries)
		{
			s_canStackCache.Clear();
		}

		s_canStackCache[instanceId] = new CanStackCacheEntry
		{
			ConfigSignature = configSignature,
			Result = result
		};
		return result;
	}

	private static int GetConfigSignature()
	{
		int signature = 0;
		if (StackConfig.ConsumablesStackable)
		{
			signature |= 1;
		}
		if (StackConfig.VehicleUpgradesStackable)
		{
			signature |= 2;
		}
		return signature;
	}

	private static bool CanStackUncached(Pickupable pickupable)
	{
		var techType = pickupable.GetTechType();
		var techId = (int)techType;

		if (IsNeverStackableTechId(techId))
			return false;

		if ((Object)(object)((Component)pickupable).GetComponent<PickupableStorage>() != (Object)null
			|| (Object)(object)((Component)pickupable).GetComponentInChildren<PickupableStorage>(true) != (Object)null)
			return false;

		if (IsAlwaysStackableTech(techType))
			return true;

		if (StackConfig.ConsumablesStackable && IsVanillaSmallCatchFishTech(techType))
			return true;

		if (StackConfig.ConsumablesStackable && IsVanillaConsumableWaterTech(techType))
			return true;

		var isEatable = (Object)(object)((Component)pickupable).GetComponentInChildren<Eatable>(true) != (Object)null;
		if (!StackConfig.ConsumablesStackable && isEatable)
			return false;

		if ((!StackConfig.ConsumablesStackable || !isEatable)
			&& ((Component)pickupable).GetComponentInChildren<IBattery>(true) != null)
			return false;

		if ((Object)(object)((Component)pickupable).GetComponentInChildren<Oxygen>(true) != (Object)null
			&& !(StackConfig.ConsumablesStackable && isEatable))
			return false;

		if ((Object)(object)((Component)pickupable).GetComponent<PlayerTool>() != (Object)null
			&& !IsStackablePlayerTool(techType))
			return false;

		if (HasBlockingWaterParkCreature(pickupable))
			return false;

		if (!StackConfig.ConsumablesStackable && techId == 4514)
			return false;

		var techName = techType.ToString();
		if (techName.IndexOf("Decoy", StringComparison.OrdinalIgnoreCase) >= 0)
			return false;

		if (techName.IndexOf("Egg", StringComparison.Ordinal) >= 0)
			return false;

		if (IsPrecursorKeyOrIonProgressionTech(techId, techName))
			return false;

		if (!StackConfig.VehicleUpgradesStackable && IsNonStackableVehicleUpgradeTech(techId, techName))
			return false;

		return true;
	}

	private static bool IsNeverStackableTechId(int techId)
	{
		return techId is 64 or 513 or 524 or 525 or 759 or 1807 or 2250 or 2251;
	}

	private static bool IsStackablePlayerTool(TechType tech)
	{
		return (int)tech is 508 or 754;
	}

	private static bool HasBlockingWaterParkCreature(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null)
			return false;

		var creatures = ((Component)pickupable).GetComponentsInChildren<WaterParkCreature>(true);
		if (creatures == null)
			return false;

		foreach (var creature in creatures)
		{
			if ((Object)(object)creature != (Object)null && ((Behaviour)creature).enabled)
				return true;
		}

		return false;
	}

	private static bool IsVanillaSmallCatchFishTech(TechType tech)
	{
		return (int)tech is 2501 or 2504 or 2505 or 2507
			or 2510 or 2513 or 2515 or 2516 or 2519 or 2520
			or 2531 or 2546 or 2554 or 2555;
	}

	private static bool IsAlwaysStackableTech(TechType tech)
	{
		return IsVanillaRawResourceTech((int)tech);
	}

	private static bool IsVanillaConsumableWaterTech(TechType tech)
	{
		return (int)tech is 4500 or 4501 or 4515 or 4516;
	}

	private static bool IsPrecursorKeyOrIonProgressionTech(int techId, string techName)
	{
		if (!string.IsNullOrEmpty(techName)
			&& techName.IndexOf("PrecursorKey", StringComparison.OrdinalIgnoreCase) >= 0)
			return true;

		return techId is 66 or 67 or 4209 or 4210;
	}

	private static bool IsNonStackableVehicleUpgradeTech(int techId, string techName)
	{
		return IsVanillaVehicleUpgradeTech(techId) || IsModVehicleUpgradeTechName(techName);
	}

	private static bool IsModVehicleUpgradeTechName(string techName)
	{
		if (string.IsNullOrEmpty(techName))
			return false;

		if (techName.StartsWith("ModVehicleDepthModule", StringComparison.Ordinal))
			return true;

		if (techName.StartsWith("ModVehicle", StringComparison.Ordinal)
			&& (techName.IndexOf("Module", StringComparison.OrdinalIgnoreCase) >= 0
				|| techName.IndexOf("Upgrade", StringComparison.OrdinalIgnoreCase) >= 0))
			return true;

		if ((techName.EndsWith("Seamoth", StringComparison.Ordinal)
			|| techName.EndsWith("Exosuit", StringComparison.Ordinal)
			|| techName.EndsWith("Cyclops", StringComparison.Ordinal))
			&& (techName.IndexOf("Module", StringComparison.OrdinalIgnoreCase) >= 0
				|| techName.IndexOf("Upgrade", StringComparison.OrdinalIgnoreCase) >= 0))
			return true;

		return false;
	}

	private static bool IsVanillaVehicleUpgradeTech(int techId)
	{
		return techId is 1516 or 1537 or 1538
			or 1551 or 1552 or 1553 or 1554 or 1555 or 1557 or 1558
			or 2101 or 2102 or 2103 or 2104 or 2109 or 2110
			or 2112 or 2113 or 2114 or 2115 or 2116 or 2117
			or 2120 or 2121 or 2122 or 2128 or 2129;
	}

	private static bool IsVanillaRawResourceTech(int techId)
	{
		return techId is 1 or 2 or 7 or 8 or 9 or 12
			or 16 or 21 or 22 or 23 or 28 or 35 or 36
			or 40 or 52 or 54 or 63 or 68 or 69
			or 3034 or 3501 or 3502 or 3504;
	}
}
