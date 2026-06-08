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
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null)
		{
			yield break;
		}
		Pickupable srcP = ((PlayerTool)source).pickupable;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<Beacon>();
		MRStack.SuppressMerge = true;
		yield return StackedPrefabFactory.Instantiate(tech, 1, spawned);
		Beacon component = spawned.Component;
		Pickupable spawnedPickup = spawned.Pickupable;
		if ((Object)(object)component == (Object)null || (Object)(object)spawnedPickup == (Object)null)
		{
			MRStack.SuppressMerge = false;
			ResetHeldBeaconUseState(source);
			yield break;
		}
		spawnedPickup.Drop(dropPos, default(Vector3), true);
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
			Sequence sequence = (Sequence)((obj is Sequence) ? obj : null);
			if (sequence != null)
			{
				sequence.Reset();
			}
		}
	}
}
