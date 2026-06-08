#nullable disable
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_UserInput), "OnDeselect")]
internal static class uGUI_UserInput_OnDeselect_StackSplitPrompt_Patch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (StackSplitPrompt.IsOpen && !Traverse.Create((object)uGUI.main.userInput).Field<bool>("submit").Value)
		{
			StackSplitPrompt.NotifyDialogClosed(submitted: false);
		}
	}
}
