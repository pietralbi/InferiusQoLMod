#nullable disable
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ToxicWater_UseSerumPickup_StackConsume_Patch
{
	public static bool Prepare()
	{
		return ToxicWaterCompat.UseSerumPickupMethod != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		return ToxicWaterCompat.UseSerumPickupMethod;
	}

	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool Prefix(object __instance, Pickupable pickupable, float duration, float cleanPercent, object tier)
	{
		if ((Object)(object)pickupable == (Object)null || !StackRules.CanStack(pickupable))
		{
			return true;
		}
		if (Stack.CountOf(pickupable) <= 1)
		{
			return true;
		}
		ToxicWaterCompat.ConsumeOneSerumUnit(__instance, pickupable, duration, cleanPercent, tier);
		return false;
	}
}
