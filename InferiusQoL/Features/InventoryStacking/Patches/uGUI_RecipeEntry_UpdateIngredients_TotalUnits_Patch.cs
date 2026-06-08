#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_RecipeEntry), "UpdateIngredients")]
[HarmonyPriority(0)]
internal static class uGUI_RecipeEntry_UpdateIngredients_TotalUnits_Patch
{
	private static readonly MethodInfo GetCount = AccessTools.Method(typeof(ItemsContainer), "GetCount", new Type[1] { typeof(TechType) }, (Type[])null);

	private static readonly MethodInfo TotalStackUnits = AccessTools.Method(typeof(CraftingCounts), "UnitsIn", new Type[2]
	{
		typeof(ItemsContainer),
		typeof(TechType)
	}, (Type[])null);

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (CodeInstructionExtensions.Calls(instruction, GetCount))
			{
				yield return new CodeInstruction(OpCodes.Call, (object)TotalStackUnits);
			}
			else
			{
				yield return instruction;
			}
		}
	}
}
