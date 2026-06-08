#nullable disable
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class ToxicWater_CountSerumInInventory_StackUnits_Patch
{
	private const string ToxicWaterTypeName = "ToxicWaterMod.ToxicWater";

	private const string CountMethodName = "CountSerumInInventory";

	public static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(AccessTools.TypeByName("ToxicWaterMod.ToxicWater"), "CountSerumInInventory", new Type[1] { typeof(TechType) }, (Type[])null);
	}

	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool Prefix(TechType targetType, ref int __result)
	{
		if ((Object)(object)Inventory.main == (Object)null || Inventory.main.container == null)
		{
			__result = 0;
			return false;
		}
		__result = ToxicWaterCompat.CountSerumStackUnits(Inventory.main.container, targetType);
		return false;
	}
}
