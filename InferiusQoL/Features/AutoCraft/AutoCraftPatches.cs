namespace InferiusQoL.Features.AutoCraft;

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using InferiusQoL.Config;

/// <summary>
/// Harmony patches for AutoCraft. All are gated through AutoCraftEnabled, so
/// disabling the feature in Options immediately returns behavior to vanilla.
///
/// Equivalent to the EasyCraft patches:
/// - GhostCrafter.Craft (Fabricator, Workbench, ...)
/// - ConstructorInput.Craft (Mobile Vehicle Bay)
/// - Constructable.Construct (green ghost object during base building)
/// - uGUI_CraftingMenu.ActionAvailable (button coloring in the menu)
/// - TooltipFactory.WriteIngredients (ingredient counts in the tooltip)
/// </summary>
[HarmonyPatch(typeof(GhostCrafter), "Craft", new[] { typeof(TechType), typeof(float) })]
internal static class AutoCraft_GhostCrafter_Craft_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(GhostCrafter __instance, TechType techType, float duration)
    {
        if (!InferiusConfig.Instance.AutoCraftEnabled) return true;
        if (!AutoCraftMain.IsGhostCrafterCraftTree(__instance.craftTree)) return true;
        AutoCraftMain.GhostCraft(__instance, techType, duration);
        return false; // suppress vanilla
    }
}

[HarmonyPatch(typeof(ConstructorInput), "Craft", new[] { typeof(TechType), typeof(float) })]
internal static class AutoCraft_ConstructorInput_Craft_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(ConstructorInput __instance, TechType techType, float duration)
    {
        if (!InferiusConfig.Instance.AutoCraftEnabled) return true;
        AutoCraftMain.ConstructorCraft(__instance, techType, duration);
        return false;
    }
}

[HarmonyPatch(typeof(Constructable), "Construct", new Type[0])]
internal static class AutoCraft_Constructable_Construct_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Constructable __instance, ref bool __result)
    {
        if (!InferiusConfig.Instance.AutoCraftEnabled) return true;
        __result = AutoCraftMain.Construct(__instance);
        return false;
    }
}

[HarmonyPatch]
internal static class AutoCraft_uGUI_CraftingMenu_ActionAvailable_Patch
{
    private static readonly Type? _nodeType = typeof(uGUI_CraftingMenu).GetNestedType("Node", BindingFlags.NonPublic);
    private static readonly FieldInfo? _idField = typeof(uGUI_CraftingMenu).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _actionField = _nodeType?.GetField("action");
    private static readonly FieldInfo? _techTypeField = _nodeType?.GetField("techType");

    private static MethodInfo? TargetMethod()
    {
        return typeof(uGUI_CraftingMenu).GetMethod("ActionAvailable", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [HarmonyPrefix]
    public static bool Prefix(uGUI_CraftingMenu __instance, ref bool __result, object sender)
    {
        if (!InferiusConfig.Instance.AutoCraftEnabled) return true;

        var id = _idField?.GetValue(__instance) as string;
        if (id == "Centrifuge" || id == "Rocket") return true;
        if (!AutoCraftMain.IsCraftingTreeSupported(__instance.treeType)) return true;

        var action = (TreeAction)(_actionField?.GetValue(sender) ?? TreeAction.None);
        var tt = (TechType)(_techTypeField?.GetValue(sender) ?? TechType.None);
        using var searchOrigin = AutoCraftHelpers.PushSearchOrigin(__instance.client as UnityEngine.Component);
        __result = action == TreeAction.Expand
            || (action == TreeAction.Craft
                && CrafterLogic.IsCraftRecipeUnlocked(tt)
                && AutoCraftMain.IsCraftRecipeFulfilledAdvanced(tt));
        return false;
    }
}

[HarmonyPatch(typeof(TooltipFactory), "WriteIngredients", new[] { typeof(IList<Ingredient>), typeof(List<TooltipIcon>) })]
internal static class AutoCraft_TooltipFactory_WriteIngredients_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(IList<Ingredient> ingredients, List<TooltipIcon> icons)
    {
        if (!InferiusConfig.Instance.AutoCraftEnabled) return true;
        if (!InferiusConfig.Instance.AutoCraftBetterTooltips) return true;
        AutoCraftMain.WriteIngredients(ingredients, icons);
        return false;
    }
}
