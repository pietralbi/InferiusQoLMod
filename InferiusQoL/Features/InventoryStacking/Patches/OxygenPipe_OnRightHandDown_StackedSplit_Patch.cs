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
		OxygenPipe ghostPipe = (OxygenPipe)((obj is OxygenPipe) ? obj : null);
		if ((Object)(object)ghostPipe == (Object)null || ghostPipe.GetParent() == null)
		{
			return true;
		}
		IPipeConnection parent = ghostPipe.GetParent();
		Vector3 position = ((Component)ghostPipe).transform.position;
		((MonoBehaviour)Player.main).StartCoroutine(CoSplitAndPlacePipe(__instance, parent, position));
		__result = true;
		return false;
	}

	private static IEnumerator CoSplitAndPlacePipe(OxygenPipe source, IPipeConnection parent, Vector3 placePos)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null || parent == null)
		{
			yield break;
		}
		Pickupable srcP = ((PlayerTool)source).pickupable;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<OxygenPipe>();
		MRStack.SuppressMerge = true;
		yield return StackedPrefabFactory.Instantiate(tech, 1, spawned);
		OxygenPipe component = spawned.Component;
		Pickupable spawnedPickup = spawned.Pickupable;
		if ((Object)(object)component == (Object)null || (Object)(object)spawnedPickup == (Object)null)
		{
			MRStack.SuppressMerge = false;
			yield break;
		}
		((Component)component).transform.position = placePos;
		spawnedPickup.SetVisible(true);
		spawnedPickup.attached = false;
		spawnedPickup.droppedEvent.Trigger(spawnedPickup);
		((Component)component).gameObject.SendMessage("OnDrop", (SendMessageOptions)1);
		component.SetParent(parent);
		if ((Object)(object)component.rigidBody != (Object)null)
		{
			InventoryStackingUnity.SetIsKinematicAndUpdateInterpolation(component.rigidBody, true, false);
		}
		object obj = GhostModelField?.GetValue(null);
		OxygenPipe ghostPipe = (OxygenPipe)((obj is OxygenPipe) ? obj : null);
		if ((Object)(object)ghostPipe != (Object)null)
		{
			((Component)ghostPipe).gameObject.SetActive(false);
		}
		MRStack.Add(srcP, -1);
		MRStack.SuppressMerge = false;
		StackIconRefresher.Trigger();
	}
}
