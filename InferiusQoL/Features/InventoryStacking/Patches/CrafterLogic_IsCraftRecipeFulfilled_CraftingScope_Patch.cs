#nullable disable
using System;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(CrafterLogic), "IsCraftRecipeFulfilled")]
internal static class CrafterLogic_IsCraftRecipeFulfilled_CraftingScope_Patch
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
