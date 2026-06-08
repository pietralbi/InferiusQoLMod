#nullable disable
using System;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackRestoreOptionsFeedback
{
	internal static void Show(string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogInfo((object)message);
			}
			if ((Object)(object)Inventory.main != (Object)null)
			{
				ErrorMessage.AddError(message);
			}
			else
			{
				TryShowOptionsDialog(message);
			}
		}
	}

	private static void TryShowOptionsDialog(string message)
	{
		try
		{
			if ((Object)(object)uGUI.main != (Object)null && (Object)(object)uGUI.main.dialog != (Object)null)
			{
				uGUI.main.dialog.Show(message, (Action<int>)null, new string[1] { "OK" });
			}
		}
		catch
		{
		}
	}
}
