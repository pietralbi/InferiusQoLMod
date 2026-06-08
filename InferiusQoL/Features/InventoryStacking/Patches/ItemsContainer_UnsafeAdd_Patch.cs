#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(ItemsContainer), "UnsafeAdd")]
internal static class ItemsContainer_UnsafeAdd_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(InventoryItem item, ItemsContainer __instance)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		if ((Object)(object)((item != null) ? item.item : null) != (Object)null && ((int)item.item.GetTechType() == 64 || ReactorFeedHelper.IsLikelyPlantableItem(item.item)))
		{
			ReactorFeedHelper.NormalizeInsertedStackToOne(item, __instance);
		}
		if ((Object)(object)((item != null) ? item.item : null) != (Object)null)
		{
			WaterParkStorageScope.EnforceSingleUnitInHabitat(item, __instance);
		}
		StackConsolidation.AfterUnsafeAdd(item, __instance);
		if ((Object)(object)((item != null) ? item.item : null) != (Object)null)
		{
			StackUidPersistence.TryApply(item, __instance);
		}
	}
}
