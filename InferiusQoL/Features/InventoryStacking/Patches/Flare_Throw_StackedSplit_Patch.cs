#nullable disable
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UWE;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Flare), "Throw")]
internal static class Flare_Throw_StackedSplit_Patch
{
	private const float ThrowPushSpeed = 8f;

	private static readonly FieldInfo IsInUseField = AccessTools.Field(typeof(PlayerTool), "_isInUse");

	private static readonly FieldInfo IsThrowingField = AccessTools.Field(typeof(Flare), "isThrowing");

	private static readonly MethodInfo SetFlareActiveStateMethod = AccessTools.Method(typeof(Flare), "SetFlareActiveState", (Type[])null, (Type[])null);

	private static readonly FieldInfo SequenceField = AccessTools.Field(typeof(Flare), "sequence");

	[HarmonyPrefix]
	private static bool Prefix(Flare __instance)
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
		((MonoBehaviour)Player.main).StartCoroutine(CoThrowOne(__instance));
		return false;
	}

	private static IEnumerator CoThrowOne(Flare source)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null)
		{
			yield break;
		}
		Pickupable srcP = ((PlayerTool)source).pickupable;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<Flare>();
		MRStack.SuppressMerge = true;
		yield return StackedPrefabFactory.Instantiate(tech, 1, spawned);
		Flare component = spawned.Component;
		Pickupable spawnedPickup = spawned.Pickupable;
		if ((Object)(object)component == (Object)null || (Object)(object)spawnedPickup == (Object)null)
		{
			MRStack.SuppressMerge = false;
			ResetHeldFlareUseState(source);
			yield break;
		}
		ThrowSingletonFlare(component, spawnedPickup);
		MRStack.Add(srcP, -1);
		MRStack.SuppressMerge = false;
		StackIconRefresher.Trigger();
		ResetHeldFlareUseState(source);
	}

	private static void ThrowSingletonFlare(Flare flare, Pickupable pickupable)
	{
		Transform cameraTransform = (((Object)(object)MainCameraControl.main != (Object)null) ? ((Component)MainCameraControl.main).transform : null);
		if (!((Object)(object)cameraTransform == (Object)null))
		{
			Vector3 forward = cameraTransform.forward;
			Vector3 dropPosition = Inventory.RayCast(cameraTransform.position, forward, 10f, 0.75f, 1.5f);
			if (IsThrowingField != null)
			{
				IsThrowingField.SetValue(flare, true);
			}
			SetFlareActiveStateMethod?.Invoke(flare, new object[1] { true });
			pickupable.Drop(dropPosition, forward * 8f, false);
			WorldForces component = ((Component)flare).GetComponent<WorldForces>();
			if ((Object)(object)component != (Object)null)
			{
				((Behaviour)component).enabled = true;
			}
			if ((Object)(object)flare.throwSound != (Object)null)
			{
				flare.throwSound.StartEvent();
			}
			Rigidbody useRigidbody = flare.useRigidbody;
			if ((Object)(object)useRigidbody != (Object)null)
			{
				InventoryStackingUnity.SetIsKinematicAndUpdateInterpolation(useRigidbody, false, false);
				useRigidbody.velocity = forward * 8f;
			}
		}
	}

	private static void ResetHeldFlareUseState(Flare source)
	{
		if (!((Object)(object)source == (Object)null))
		{
			if (IsInUseField != null)
			{
				IsInUseField.SetValue(source, false);
			}
			if (IsThrowingField != null)
			{
				IsThrowingField.SetValue(source, false);
			}
			SetFlareActiveStateMethod?.Invoke(source, new object[1] { false });
			object obj = SequenceField?.GetValue(source);
			Sequence sequence = (Sequence)((obj is Sequence) ? obj : null);
			if (sequence != null)
			{
				sequence.Reset();
			}
		}
	}
}
