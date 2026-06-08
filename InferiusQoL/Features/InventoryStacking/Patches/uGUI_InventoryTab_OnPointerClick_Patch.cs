#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_InventoryTab), "OnPointerClick")]
internal static class uGUI_InventoryTab_OnPointerClick_Patch
{
	private static bool IsConfiguredKeyHeld(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Invalid comparison between Unknown and I4
		if ((int)key == 0)
		{
			return false;
		}
		if (Input.GetKey(key))
		{
			return true;
		}
		if ((int)key == 308 && Input.GetKey((KeyCode)307))
		{
			return true;
		}
		if ((int)key == 307 && Input.GetKey((KeyCode)308))
		{
			return true;
		}
		if ((int)key == 306 && Input.GetKey((KeyCode)305))
		{
			return true;
		}
		if ((int)key == 305 && Input.GetKey((KeyCode)306))
		{
			return true;
		}
		if ((int)key == 304 && Input.GetKey((KeyCode)303))
		{
			return true;
		}
		if ((int)key == 303 && Input.GetKey((KeyCode)304))
		{
			return true;
		}
		return false;
	}

	private static bool IsUsingGamepad()
	{
		try
		{
			return GameInput.IsPrimaryDeviceGamepad();
		}
		catch
		{
			return false;
		}
	}

	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool Prefix(InventoryItem item, int button)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		if (ItemDragManager.isDragging || item == null || (Object)(object)Inventory.main == (Object)null)
		{
			return true;
		}
		if (IsUsingGamepad())
		{
			return true;
		}
		bool splitPromptHeld = IsConfiguredKeyHeld(StackConfig.SplitPromptKey);
		bool moveHalfHeld = IsConfiguredKeyHeld(StackConfig.MoveHalfKey);
		bool mergeAllHeld = IsConfiguredKeyHeld(StackConfig.MergeAllKey);
		if (button == 0 && mergeAllHeld && !splitPromptHeld && !moveHalfHeld)
		{
			if (StackConsolidation.TryMergeAllLike(item))
			{
				return false;
			}
			return true;
		}
		IItemsContainer oppositeContainer = Inventory.main.GetOppositeContainer(item);
		if (button == 0 && (Object)(object)item.item != (Object)null && MRStack.CountOf(item.item) > 1 && PartialTransferOne.IsSingleUnitInsertTarget(oppositeContainer) && !ReactorFeedHelper.IsReactorContainer(oppositeContainer))
		{
			if (PartialTransferOne.TryStart(item))
			{
				return false;
			}
			return true;
		}
		if ((button == 0 || button == 1) && (Object)(object)item.item != (Object)null && MRStack.CountOf(item.item) > 1 && ReactorFeedHelper.IsReactorContainer(oppositeContainer))
		{
			if (PartialTransferOne.TryStart(item))
			{
				return false;
			}
			return true;
		}
		bool flag = splitPromptHeld;
		if (button == 0 && flag)
		{
			if ((Object)(object)item.item != (Object)null && (object)item.container == Inventory.main.container && MRStack.CountOf(item.item) > 1)
			{
				StackSplitPrompt.TryOpen(item);
				return false;
			}
			return true;
		}
		bool flag2 = moveHalfHeld;
		if (button == 0 && flag2 && ((int)Inventory.main.GetAllItemActions(item) & 0x20) != 0)
		{
			if (MRStack.CountOf(item.item) > 1)
			{
				PartialTransferOne.TryStartHalf(item);
			}
			return false;
		}
		if (button != 1)
		{
			return true;
		}
		if (((int)Inventory.main.GetAllItemActions(item) & 0x20) == 0)
		{
			return true;
		}
		if (MRStack.CountOf(item.item) <= 1)
		{
			if (StackRules.CanStack(item.item))
			{
				Inventory.main.ExecuteItemAction((ItemAction)32, item);
				return false;
			}
			return true;
		}
		PartialTransferOne.TryStart(item);
		return false;
	}
}
