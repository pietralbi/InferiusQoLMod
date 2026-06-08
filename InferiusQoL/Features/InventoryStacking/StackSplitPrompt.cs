#nullable disable
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackSplitPrompt
{
	private static bool s_open;

	internal static bool IsOpen => s_open;

	internal static void NotifyDialogClosed(bool submitted)
	{
		s_open = false;
	}

	public static void TryOpen(InventoryItem source)
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		if (s_open)
		{
			return;
		}
		InventoryItem obj = source;
		if ((Object)(object)((obj != null) ? obj.item : null) == (Object)null || (Object)(object)Inventory.main == (Object)null || (Object)(object)uGUI.main?.userInput == (Object)null || (object)source.container != Inventory.main.container || !StackRules.CanStack(source.item))
		{
			return;
		}
		int num = MRStack.CountOf(source.item);
		if (num <= 1)
		{
			return;
		}
		int max = num - 1;
		s_open = true;
		uGUI.main.userInput.RequestString($"Split stack (1-{max})", "Create", "1", 6, (uGUI_UserInput.UserInputCallback)delegate(string text)
		{
			NotifyDialogClosed(submitted: true);
			if (!int.TryParse((text ?? string.Empty).Trim(), out var result))
			{
				ErrorMessage.AddError("Enter a valid number.");
			}
			else if (result < 1 || result > max)
			{
				ErrorMessage.AddError($"Split amount must be between 1 and {max}.");
			}
			else if (!PartialTransferOne.TrySplitInSameContainer(source, result))
			{
				ErrorMessage.AddError("Could not split this stack.");
			}
		});
	}
}
