#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

using System;
using System.Reflection;
using HarmonyLib;

internal static class StackUiAccess
{
	private static readonly object[] s_noArgs = Array.Empty<object>();

	private static readonly FieldInfo ItemsContainerField = AccessTools.Field(typeof(uGUI_ItemsContainer), "container");

	private static readonly FieldInfo QuickSlotIconsField = AccessTools.Field(typeof(uGUI_QuickSlots), "icons");

	private static readonly MethodInfo QuickSlotsGetTargetMethod = AccessTools.Method(typeof(uGUI_QuickSlots), "GetTarget", Type.EmptyTypes, null);

	public static ItemsContainer GetContainer(uGUI_ItemsContainer view)
	{
		return ItemsContainerField?.GetValue(view) as ItemsContainer;
	}

	public static uGUI_ItemIcon[] GetQuickSlotIcons(uGUI_QuickSlots quickSlots)
	{
		return QuickSlotIconsField?.GetValue(quickSlots) as uGUI_ItemIcon[];
	}

	public static IQuickSlots GetQuickSlotsTarget(uGUI_QuickSlots quickSlots)
	{
		return QuickSlotsGetTargetMethod?.Invoke(quickSlots, s_noArgs) as IQuickSlots;
	}
}
