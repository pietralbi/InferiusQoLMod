#nullable disable
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UWE;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(OxygenPipe), "OnRightHandDown")]
internal static class OxygenPipe_OnRightHandDown_StackedSplit_Patch
{
	private static readonly FieldInfo GhostModelField = AccessTools.Field(typeof(OxygenPipe), "ghostModel");

	[HarmonyPrefix]
	private static bool Prefix(OxygenPipe __instance, ref bool __result)
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)((PlayerTool)__instance).pickupable == (Object)null)
		{
			return true;
		}
		if ((Object)(object)Player.main == (Object)null)
		{
			return true;
		}
		if (Player.main.IsBleederAttached())
		{
			return true;
		}
		Pickupable pickupable = ((PlayerTool)__instance).pickupable;
		if (!StackRules.CanStack(pickupable))
		{
			return true;
		}
		if (MRStack.CountOf(pickupable) <= 1)
		{
			return true;
		}
		object obj = GhostModelField?.GetValue(null);
		OxygenPipe val = (OxygenPipe)((obj is OxygenPipe) ? obj : null);
		if ((Object)(object)val == (Object)null || val.GetParent() == null)
		{
			return true;
		}
		IPipeConnection parent = val.GetParent();
		Vector3 position = ((Component)val).transform.position;
		((MonoBehaviour)Player.main).StartCoroutine(CoSplitAndPlacePipe(__instance, parent, position));
		__result = true;
		return false;
	}

	private static IEnumerator CoSplitAndPlacePipe(OxygenPipe source, IPipeConnection parent, Vector3 placePos)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null || parent == null)
		{
			yield break;
		}
		Pickupable srcP = ((PlayerTool)source).pickupable;
		TechType tech = srcP.GetTechType();
		TaskResult<GameObject> result = new TaskResult<GameObject>();
		MRStack.SuppressMerge = true;
		yield return CraftData.InstantiateFromPrefabAsync(tech, (IOut<GameObject>)(object)result, false);
		GameObject val = result.Get();
		if ((Object)(object)val == (Object)null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		OxygenPipe component = val.GetComponent<OxygenPipe>();
		Pickupable val2 = (((Object)(object)component != (Object)null) ? ((PlayerTool)component).pickupable : null);
		if ((Object)(object)component == (Object)null || (Object)(object)val2 == (Object)null)
		{
			Object.Destroy((Object)(object)val);
			MRStack.SuppressMerge = false;
			yield break;
		}
		CrafterLogic.NotifyCraftEnd(val, tech);
		MRStack.SetAmount(val2, 1);
		((Component)component).transform.position = placePos;
		val2.SetVisible(true);
		val2.attached = false;
		val2.droppedEvent.Trigger(val2);
		((Component)component).gameObject.SendMessage("OnDrop", (SendMessageOptions)1);
		component.SetParent(parent);
		if ((Object)(object)component.rigidBody != (Object)null)
		{
			InventoryStackingUnity.SetIsKinematicAndUpdateInterpolation(component.rigidBody, true, false);
		}
		object obj = GhostModelField?.GetValue(null);
		OxygenPipe val3 = (OxygenPipe)((obj is OxygenPipe) ? obj : null);
		if ((Object)(object)val3 != (Object)null)
		{
			((Component)val3).gameObject.SetActive(false);
		}
		MRStack.Add(srcP, -1);
		MRStack.SuppressMerge = false;
		StackIconRefresher.Trigger();
	}
}
