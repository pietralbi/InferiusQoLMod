#nullable disable
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Beacon), "Throw")]
internal static class Beacon_Throw_StackedSplit_Patch
{
	private static readonly FieldInfo IsInUseField = AccessTools.Field(typeof(PlayerTool), "_isInUse");

	private static readonly MethodInfo SetBeaconActiveStateMethod = AccessTools.Method(typeof(Beacon), "SetBeaconActiveState", (Type[])null, (Type[])null);

	private static readonly FieldInfo SequenceField = AccessTools.Field(typeof(Beacon), "sequence");

	[HarmonyPrefix]
	private static bool Prefix(Beacon __instance)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || (Object)(object)((PlayerTool)__instance).pickupable == (Object)null || (Object)(object)Player.main == (Object)null)
		{
			return true;
		}
		Pickupable pickupable = ((PlayerTool)__instance).pickupable;
		if (!StackRules.CanStack(pickupable) || MRStack.CountOf(pickupable) <= 1)
		{
			return true;
		}
		Vector3 position = ((Component)__instance).transform.position;
		Quaternion rotation = Quaternion.LookRotation(((Component)Player.main).transform.position);
		((MonoBehaviour)Player.main).StartCoroutine(CoThrowOne(__instance, position, rotation));
		return false;
	}

	private static IEnumerator CoThrowOne(Beacon source, Vector3 dropPos, Quaternion rotation)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null)
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
			ResetHeldBeaconUseState(source);
			yield break;
		}
		Beacon component = val.GetComponent<Beacon>();
		Pickupable val2 = (((Object)(object)component != (Object)null) ? ((PlayerTool)component).pickupable : null);
		if ((Object)(object)component == (Object)null || (Object)(object)val2 == (Object)null)
		{
			Object.Destroy((Object)(object)val);
			MRStack.SuppressMerge = false;
			ResetHeldBeaconUseState(source);
			yield break;
		}
		CrafterLogic.NotifyCraftEnd(val, tech);
		MRStack.SetAmount(val2, 1);
		val2.Drop(dropPos, default(Vector3), true);
		((Component)component).transform.rotation = rotation;
		WorldForces component2 = ((Component)component).GetComponent<WorldForces>();
		if ((Object)(object)component2 != (Object)null)
		{
			((Behaviour)component2).enabled = true;
		}
		if ((Object)(object)component.beaconOnLoop != (Object)null)
		{
			component.beaconOnLoop.Play();
		}
		MRStack.Add(srcP, -1);
		MRStack.SuppressMerge = false;
		StackIconRefresher.Trigger();
		ResetHeldBeaconUseState(source);
	}

	private static void ResetHeldBeaconUseState(Beacon source)
	{
		if (!((Object)(object)source == (Object)null))
		{
			if (IsInUseField != null)
			{
				IsInUseField.SetValue(source, false);
			}
			SetBeaconActiveStateMethod?.Invoke(source, new object[1] { false });
			object obj = SequenceField?.GetValue(source);
			Sequence val = (Sequence)((obj is Sequence) ? obj : null);
			if (val != null)
			{
				val.Reset();
			}
		}
	}
}
