#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class EasyCraft_Main_ConsumeIngredients_DuplicateGuard
{
	private const string MainTypeName = "EasyCraft.Main";

	private const string MethodName = "ConsumeIngredients";

	public static bool Prepare()
	{
		Type type = AccessTools.TypeByName("EasyCraft.Main");
		if (type == null)
		{
			return false;
		}
		return AccessTools.Method(type, "ConsumeIngredients", new Type[1] { typeof(Dictionary<TechType, int>) }, (Type[])null) != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase Target()
	{
		return AccessTools.Method(AccessTools.TypeByName("EasyCraft.Main"), "ConsumeIngredients", new Type[1] { typeof(Dictionary<TechType, int>) }, (Type[])null);
	}

	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static bool Prefix()
	{
		return CraftingConsumeGuard.TryClaim("EasyCraft");
	}
}
