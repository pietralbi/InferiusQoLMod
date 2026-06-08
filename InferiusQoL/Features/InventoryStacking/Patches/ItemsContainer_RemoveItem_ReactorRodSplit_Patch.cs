#nullable disable
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(ItemsContainer), "RemoveItem", new Type[] { typeof(TechType) })]
internal static class ItemsContainer_RemoveItem_ReactorRodSplit_Patch
{
	public static bool Prepare()
	{
		return false;
	}

	[HarmonyPostfix]
	private static void Postfix(ItemsContainer __instance, TechType techType, ref Pickupable __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		if ((int)techType != 64 || __instance == null || (Object)(object)__result == (Object)null)
		{
			return;
		}
		int num = MRStack.CountOf(__result);
		if (num <= 1)
		{
			return;
		}
		int count = num - 1;
		MRStack.Ensure(__result, 1);
		GameObject val = Object.Instantiate<GameObject>(((Component)__result).gameObject);
		Pickupable val2 = (((Object)(object)val == (Object)null) ? null : val.GetComponent<Pickupable>());
		if ((Object)(object)val2 == (Object)null)
		{
			if ((Object)(object)val != (Object)null)
			{
				Object.Destroy((Object)(object)val);
			}
			return;
		}
		MRStack.Ensure(val2, count);
		if (__instance.AddItem(val2) == null && (Object)(object)Player.main != (Object)null)
		{
			Vector3 val3 = ((Component)Player.main).transform.position + ((Component)Player.main).transform.forward * 1.2f;
			val2.Drop(val3, default(Vector3), true);
		}
	}
}
