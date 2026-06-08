#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(TooltipFactory), "WriteIngredients", new Type[]
{
	typeof(IList<Ingredient>),
	typeof(List<TooltipIcon>)
})]
[HarmonyPriority(0)]
internal static class TooltipFactory_WriteIngredients_UnitsTranspiler
{
	private static readonly MethodInfo PickupUnitsForCraft = AccessTools.Method(typeof(CraftingCounts), "PickupUnitsForCraft", new Type[2]
	{
		typeof(Inventory),
		typeof(TechType)
	}, (Type[])null);

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (IsInventoryGetPickupCountCall(instruction))
			{
				yield return new CodeInstruction(OpCodes.Call, (object)PickupUnitsForCraft);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	private static bool IsInventoryGetPickupCountCall(CodeInstruction ins)
	{
		if (ins.opcode != OpCodes.Call && ins.opcode != OpCodes.Callvirt)
		{
			return false;
		}
		if (ins.operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(Inventory))
		{
			return methodInfo.Name == "GetPickupCount";
		}
		return false;
	}
}
