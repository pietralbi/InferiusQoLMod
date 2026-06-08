#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using InferiusQoL.Features.InventoryStacking.Patches;

namespace InferiusQoL.Features.InventoryStacking;

internal static class HarmonyPatchDiagnostics
{
	internal static void LogCraftingAndStackingPatches(ManualLogSource log)
	{
		if (log == null)
		{
			return;
		}
		try
		{
			LogPatch(log, "stack merge", AccessTools.Method(typeof(ItemsContainer), "UnsafeAdd", (Type[])null, (Type[])null));
			MethodInfo method = AccessTools.Method(typeof(TooltipFactory), "WriteIngredients", new Type[2]
			{
				typeof(IList<Ingredient>),
				typeof(List<TooltipIcon>)
			}, (Type[])null);
			LogPatch(log, "fabricator tooltip ingredients", method);
			LogPatch(log, "craftability check", AccessTools.Method(typeof(CrafterLogic), "IsCraftRecipeFulfilled", (Type[])null, (Type[])null));
			LogPatch(log, "pinned recipe counts", AccessTools.Method(typeof(uGUI_RecipeEntry), "UpdateIngredients", (Type[])null, (Type[])null));
			LogPatch(log, "inventory pickup count (crafting scope)", AccessTools.Method(typeof(Inventory), "GetPickupCount", (Type[])null, (Type[])null));
			LogPatch(log, "oxygen pipe place (single-unit consume)", AccessTools.Method(typeof(OxygenPipe), "OnRightHandDown", (Type[])null, (Type[])null));
			LogPatch(log, "beacon throw (single-unit consume)", AccessTools.Method(typeof(Beacon), "Throw", (Type[])null, (Type[])null));
			LogPatch(log, "flare throw (single-unit consume)", AccessTools.Method(typeof(Flare), "Throw", (Type[])null, (Type[])null));
			LogPatch(log, "led light place (single-unit consume)", AccessTools.Method(typeof(PlaceTool), "Place", (Type[])null, (Type[])null));
			LogOptionalCompat(log, "EasyCraft pickup count", "EasyCraft.ClosestItemContainers", "GetPickupCount", new Type[1] { typeof(TechType) });
			LogOptionalCompat(log, "EasyCraft consume ingredients", "EasyCraft.ClosestItemContainers", "DestroyItem", new Type[2]
			{
				typeof(TechType),
				typeof(int)
			});
			LogOptionalCompat(log, "EasyCraft duplicate-consume guard", "EasyCraft.Main", "ConsumeIngredients", new Type[1] { typeof(Dictionary<TechType, int>) });
			LogOptionalCompat(log, "InferiusQoL pickup count", "InferiusQoL.Features.AutoCraft.ClosestItemContainers", "GetPickupCount", new Type[1] { typeof(TechType) });
			LogOptionalCompat(log, "InferiusQoL consume ingredients", "InferiusQoL.Features.AutoCraft.ClosestItemContainers", "DestroyItem", new Type[2]
			{
				typeof(TechType),
				typeof(int)
			});
			LogOptionalCompat(log, "InferiusQoL duplicate-consume guard", "InferiusQoL.Features.AutoCraft.AutoCraftMain", "ConsumeIngredients", new Type[1] { typeof(Dictionary<TechType, int>) });
			LogOptionalCompat(log, "Resource Monitor per-locker pull", "ResourceMonitor.Components.ResourceMonitorLogic", "AttemptToTakeItem", new Type[1] { typeof(TechType) });
			LogOptionalCompat(log, "Toxic Water serum counter", "ToxicWaterMod.ToxicWater", "CountSerumInInventory", new Type[1] { typeof(TechType) });
			if (ToxicWaterCompat.UseSerumPickupMethod != null)
			{
				LogPatch(log, "Toxic Water serum use (single-unit)", ToxicWaterCompat.UseSerumPickupMethod);
			}
			else
			{
				log.LogInfo((object)"[InventoryStacking] Toxic Water serum use (single-unit): target mod not installed (skipped)");
			}
		}
		catch (Exception ex)
		{
			log.LogWarning((object)("Patch diagnostics failed: " + ex.Message));
		}
	}

	internal static void LogResourceMonitorPatches(ManualLogSource log)
	{
		if (log != null && ResourceMonitorCompat.IsAvailable)
		{
			LogPatch(log, "Resource Monitor tracker reconcile", ResourceMonitorCompat.AddItemsToTrackerMethod);
			LogPatch(log, "Resource Monitor per-locker pull (resolved)", ResourceMonitorCompat.AttemptToTakeItemMethod);
		}
	}

	private static void LogOptionalCompat(ManualLogSource log, string label, string typeName, string methodName, Type[] paramTypes)
	{
		Type type = AccessTools.TypeByName(typeName);
		if (type == null)
		{
			log.LogInfo((object)("[InventoryStacking] " + label + ": target mod not installed (skipped)"));
		}
		else
		{
			LogPatch(log, label, AccessTools.Method(type, methodName, paramTypes, (Type[])null));
		}
	}

	private static void LogPatch(ManualLogSource log, string label, MethodBase method)
	{
		if (method == null)
		{
			log.LogWarning((object)("[InventoryStacking] Patch target missing: " + label));
			return;
		}
		HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(method);
		int num = (patchInfo?.Prefixes?.Count).GetValueOrDefault() + (patchInfo?.Postfixes?.Count).GetValueOrDefault() + (patchInfo?.Transpilers?.Count).GetValueOrDefault() + (patchInfo?.Finalizers?.Count).GetValueOrDefault();
		if (num == 0)
		{
			log.LogWarning((object)("[InventoryStacking] No Harmony patches on " + label + " (" + method.DeclaringType?.Name + "." + method.Name + ")"));
		}
		else
		{
			string arg = (patchInfo.Prefixes.Concat(patchInfo.Postfixes).Concat(patchInfo.Transpilers).Concat(patchInfo.Finalizers)
				.Any((Patch p) => p.owner == global::InferiusQoL.Plugin.HarmonyId) ? "includes InferiusQoL" : "no InferiusQoL owner");
			log.LogInfo((object)$"[InventoryStacking] {label}: {num} patch(es) ({arg})");
		}
	}
}
