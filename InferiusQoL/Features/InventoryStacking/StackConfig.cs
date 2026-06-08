#nullable disable
using System;
using InferiusQoL.Config;
using UnityEngine;

namespace InferiusQoL.Features.InventoryStacking;

public static class StackConfig
{
	public const int MinStack = 2;

	public const int MaxStack = 100;

	public const int DefaultMaxStack = 10;

	public const float DefaultSplitMergeProtectionSeconds = 10f;

	public static int MaxStackSize
	{
		get
		{
			return Mathf.Clamp(InferiusConfig.Instance.InventoryStackingMaxStackSize, 2, 100);
		}
	}

	public static float SplitMergeProtectionSeconds
	{
		get
		{
			return Mathf.Clamp(InferiusConfig.Instance.InventoryStackingSplitMergeProtectionSeconds, 2f, 60f);
		}
	}

	public static KeyCode SplitPromptKey => ToKeyCode(InferiusConfig.Instance.InventoryStackingSplitPromptKey, KeyCode.LeftAlt);

	public static KeyCode MoveHalfKey => ToKeyCode(InferiusConfig.Instance.InventoryStackingMoveHalfKey, KeyCode.LeftControl);

	public static KeyCode MergeAllKey => ToKeyCode(InferiusConfig.Instance.InventoryStackingMergeAllKey, KeyCode.LeftShift);

	public static bool ConsumablesStackable => InferiusConfig.Instance.InventoryStackingConsumablesStackable;

	public static bool VehicleUpgradesStackable => InferiusConfig.Instance.InventoryStackingVehicleUpgradesStackable;

	private static KeyCode ToKeyCode(string key, KeyCode fallback)
	{
		return Enum.TryParse<KeyCode>(key, ignoreCase: true, out var parsed)
			? parsed
			: fallback;
	}
}
