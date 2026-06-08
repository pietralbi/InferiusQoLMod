#nullable disable
using System;
using System.Collections.Generic;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(TooltipFactory), "WriteIngredients", new Type[]
{
	typeof(IList<Ingredient>),
	typeof(List<TooltipIcon>)
})]
internal static class TooltipFactory_WriteIngredients_CraftingScope_Patch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		CraftingCounts.EnterCraftingQuery();
	}

	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception)
	{
		CraftingCounts.ExitCraftingQuery();
		return __exception;
	}
}
