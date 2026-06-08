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
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		LEDLight val = (LEDLight)(object)((__instance is LEDLight) ? __instance : null);
		if (val == null || (Object)(object)((PlayerTool)val).pickupable == (Object)null || (Object)(object)Player.main == (Object)null)
		{
			return true;
		}
		if (!(ValidPositionField != null) || !(bool)ValidPositionField.GetValue(__instance))
		{
			return true;
		}
		Pickupable pickupable = ((PlayerTool)val).pickupable;
		if (!StackRules.CanStack(pickupable) || MRStack.CountOf(pickupable) <= 1)
		{
			return true;
		}
		object obj = GhostModelField?.GetValue(__instance);
		GameObject val2 = (GameObject)((obj is GameObject) ? obj : null);
		if ((Object)(object)val2 == (Object)null)
		{
			return true;
		}
		Vector3 position = val2.transform.position;
		Quaternion rotation = val2.transform.rotation;
		((MonoBehaviour)Player.main).StartCoroutine(CoSplitAndPlace(val, __instance, position, rotation));
		__result = true;
		return false;
	}

	private static IEnumerator CoSplitAndPlace(LEDLight source, PlaceTool sourcePlace, Vector3 position, Quaternion rotation)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
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
			yield break;
		}
		LEDLight component = val.GetComponent<LEDLight>();
		Pickupable val2 = (((Object)(object)component != (Object)null) ? ((PlayerTool)component).pickupable : null);
		if ((Object)(object)component == (Object)null || (Object)(object)val2 == (Object)null)
		{
			Object.Destroy((Object)(object)val);
			MRStack.SuppressMerge = false;
			yield break;
		}
		CrafterLogic.NotifyCraftEnd(val, tech);
		MRStack.SetAmount(val2, 1);
		val2.Drop(position, Vector3.zero, false);
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
		MRStack.Add(srcP, -1);
		MRStack.SuppressMerge = false;
		StackIconRefresher.Trigger();
	}
}
