#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
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

	private static readonly Dictionary<int, float> s_pendingStackSplitUntilTime = new Dictionary<int, float>();

	private static readonly FieldInfo IsInUseField = AccessTools.Field(typeof(PlayerTool), "_isInUse");

	private static readonly FieldInfo IsThrowingField = AccessTools.Field(typeof(Flare), "isThrowing");

	private static readonly MethodInfo SetFlareActiveStateMethod = AccessTools.Method(typeof(Flare), "SetFlareActiveState", (Type[])null, (Type[])null);

	private static readonly FieldInfo SequenceField = AccessTools.Field(typeof(Flare), "sequence");

	private static readonly MethodInfo LoopingSoundStopMethod = AccessTools.Method(AccessTools.TypeByName("FMOD_CustomLoopingEmitter"), "Stop");

	internal static void RecordPendingStackSplit(Flare source)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null)
		{
			return;
		}

		Pickupable pickupable = ((PlayerTool)source).pickupable;
		if (!StackFlareState.IsUnusedFlare(pickupable) || Stack.CountOf(pickupable) <= 1)
		{
			return;
		}

		float lifetime = Mathf.Max(0.5f, source.throwDuration + 1f);
		s_pendingStackSplitUntilTime[((Object)((Component)source).gameObject).GetInstanceID()] = Time.time + lifetime;
	}

	[HarmonyPrefix]
	private static bool Prefix(Flare __instance)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)((PlayerTool)__instance).pickupable == (Object)null || (Object)(object)Player.main == (Object)null)
		{
			return true;
		}
		Pickupable pickupable = ((PlayerTool)__instance).pickupable;
		if (!StackFlareState.IsFlare(pickupable) || Stack.CountOf(pickupable) <= 1 || !TryConsumePendingStackSplit(__instance))
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
		float fallbackEnergyLeft = source.energyLeft;
		var spawned = new StackedPrefab<Flare>();
		Stack.SuppressMerge = true;
		yield return StackedPrefabFactory.Instantiate(tech, 1, spawned);
		Flare component = spawned.Component;
		Pickupable spawnedPickup = spawned.Pickupable;
		if ((Object)(object)component == (Object)null || (Object)(object)spawnedPickup == (Object)null)
		{
			Stack.SuppressMerge = false;
			ResetHeldFlareUseState(source, fallbackEnergyLeft);
			yield break;
		}
		float unusedEnergyLeft = component.energyLeft;
		ThrowSingletonFlare(component, spawnedPickup);
		Stack.Add(srcP, -1);
		Stack.SuppressMerge = false;
		StackIconRefresher.Trigger();
		ResetHeldFlareUseState(source, unusedEnergyLeft);
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

	private static bool TryConsumePendingStackSplit(Flare source)
	{
		if ((Object)(object)source == (Object)null)
		{
			return false;
		}

		int instanceId = ((Object)((Component)source).gameObject).GetInstanceID();
		if (!s_pendingStackSplitUntilTime.TryGetValue(instanceId, out float untilTime))
		{
			return false;
		}

		s_pendingStackSplitUntilTime.Remove(instanceId);
		return Time.time <= untilTime;
	}

	private static void ResetHeldFlareUseState(Flare source, float unusedEnergyLeft)
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
			StopLoopingSound(source);
			if ((Object)(object)source.fxControl != (Object)null && source.fxIsPlaying)
			{
				source.fxControl.StopAndDestroy(1, 0f);
				source.fxIsPlaying = false;
			}
			SetFlareActiveStateMethod?.Invoke(source, new object[1] { false });
			source.hasBeenThrown = false;
			source.flareActiveState = false;
			source.flareActivateTime = 0f;
			source.energyLeft = unusedEnergyLeft;
			if ((Object)(object)source.capRenderer != (Object)null)
			{
				source.capRenderer.enabled = true;
			}
			if ((Object)(object)source.light != (Object)null)
			{
				source.light.intensity = 0f;
				source.light.range = 0f;
				source.light.enabled = false;
			}
			object obj = SequenceField?.GetValue(source);
			Sequence sequence = (Sequence)((obj is Sequence) ? obj : null);
			if (sequence != null)
			{
				sequence.Reset();
			}
		}
	}

	private static void StopLoopingSound(Flare source)
	{
		object loopingSound = source.loopingSound;
		if (loopingSound == null)
		{
			return;
		}

		MethodInfo stopMethod = LoopingSoundStopMethod ?? loopingSound.GetType().GetMethod("Stop", BindingFlags.Instance | BindingFlags.Public);
		if (stopMethod == null)
		{
			return;
		}

		ParameterInfo[] parameters = stopMethod.GetParameters();
		object[] args = new object[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Type.Missing;
		}
		stopMethod.Invoke(loopingSound, args);
	}
}

[HarmonyPatch(typeof(Flare), "OnToolUseAnim")]
internal static class Flare_OnToolUseAnim_StackedSplitMarker_Patch
{
	[HarmonyPrefix]
	private static void Prefix(Flare __instance)
	{
		Flare_Throw_StackedSplit_Patch.RecordPendingStackSplit(__instance);
	}
}
