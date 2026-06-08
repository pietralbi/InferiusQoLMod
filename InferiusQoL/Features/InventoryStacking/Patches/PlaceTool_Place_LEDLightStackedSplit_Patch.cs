#nullable disable
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UWE;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(PlaceTool), "Place")]
internal static class PlaceTool_Place_LEDLightStackedSplit_Patch
{
	private static readonly FieldInfo GhostModelField = AccessTools.Field(typeof(PlaceTool), "ghostModel");

	private static readonly FieldInfo ValidPositionField = AccessTools.Field(typeof(PlaceTool), "validPosition");

	private static readonly FieldInfo UsedThisFrameField = AccessTools.Field(typeof(PlaceTool), "usedThisFrame");

	[HarmonyPrefix]
	private static bool Prefix(PlaceTool __instance, ref bool __result)
	{
		LEDLight ledLight = (LEDLight)(object)((__instance is LEDLight) ? __instance : null);
		if (ledLight == null || (Object)(object)((PlayerTool)ledLight).pickupable == (Object)null || (Object)(object)Player.main == (Object)null)
		{
			return true;
		}
		if (!(ValidPositionField != null) || !(bool)ValidPositionField.GetValue(__instance))
		{
			return true;
		}
		Pickupable pickupable = ((PlayerTool)ledLight).pickupable;
		if (!StackRules.CanStack(pickupable) || Stack.CountOf(pickupable) <= 1)
		{
			return true;
		}
		object obj = GhostModelField?.GetValue(__instance);
		GameObject ghostModel = (GameObject)((obj is GameObject) ? obj : null);
		if ((Object)(object)ghostModel == (Object)null)
		{
			return true;
		}
		Vector3 position = ghostModel.transform.position;
		Quaternion rotation = ghostModel.transform.rotation;
		((MonoBehaviour)Player.main).StartCoroutine(CoSplitAndPlace(ledLight, __instance, position, rotation));
		__result = true;
		return false;
	}

	private static IEnumerator CoSplitAndPlace(LEDLight source, PlaceTool sourcePlace, Vector3 position, Quaternion rotation)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)((PlayerTool)source).pickupable == (Object)null)
		{
			yield break;
		}
		Pickupable srcP = ((PlayerTool)source).pickupable;
		TechType tech = srcP.GetTechType();
		var spawned = new StackedPrefab<LEDLight>();
		Stack.SuppressMerge = true;
		yield return StackedPrefabFactory.Instantiate(tech, 1, spawned);
		LEDLight component = spawned.Component;
		Pickupable spawnedPickup = spawned.Pickupable;
		if ((Object)(object)component == (Object)null || (Object)(object)spawnedPickup == (Object)null)
		{
			Stack.SuppressMerge = false;
			yield break;
		}
		spawnedPickup.Drop(position, Vector3.zero, false);
		((Component)component).transform.position = position;
		((Component)component).transform.rotation = rotation;
		SubRoot currentSub = Player.main.GetCurrentSub();
		if ((Object)(object)currentSub != (Object)null)
		{
			((Component)component).transform.parent = currentSub.GetModulesRoot();
			if ((Object)(object)component.lwe != (Object)null)
			{
				((Behaviour)component.lwe).enabled = false;
			}
		}
		SkyEnvironmentChanged.Send(((Component)component).gameObject, (Component)(object)currentSub);
		if ((Object)(object)component.rigidBody != (Object)null)
		{
			InventoryStackingUnity.SetIsKinematicAndUpdateInterpolation(component.rigidBody, true, false);
		}
		if ((Object)(object)sourcePlace.placementSound != (Object)null)
		{
			Utils.PlayFMODAsset(sourcePlace.placementSound, ((Component)component).transform, 20f);
		}
		((PlaceTool)component).OnPlace();
		if (UsedThisFrameField != null)
		{
			UsedThisFrameField.SetValue(sourcePlace, false);
		}
		Stack.Add(srcP, -1);
		Stack.SuppressMerge = false;
		StackIconRefresher.Trigger();
	}
}
