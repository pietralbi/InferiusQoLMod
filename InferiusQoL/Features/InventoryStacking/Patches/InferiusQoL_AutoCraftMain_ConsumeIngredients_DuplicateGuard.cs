#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class InferiusQoL_AutoCraftMain_ConsumeIngredients_DuplicateGuard
{
	private const string MethodName = "ConsumeIngredients";

	public static bool Prepare()
	{
		Type mainType = InferiusQoLCompat.MainType;
		if (mainType == null)
		{
			return false;
		}
		return AccessTools.Method(mainType, "ConsumeIngredients", new Type[1] { typeof(Dictionary<TechType, int>) }, (Type[])null) != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase Target()
	{
		return AccessTools.Method(InferiusQoLCompat.MainType, "ConsumeIngredients", new Type[1] { typeof(Dictionary<TechType, int>) }, (Type[])null);
	}

	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static bool Prefix()
	{
		return CraftingConsumeGuard.TryClaim("InferiusQoL");
	}
}
