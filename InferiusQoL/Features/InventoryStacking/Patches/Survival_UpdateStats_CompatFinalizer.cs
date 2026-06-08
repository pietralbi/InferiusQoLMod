#nullable disable
using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(Survival), "UpdateStats")]
internal static class Survival_UpdateStats_CompatFinalizer
{
	private static readonly MethodInfo UpdateWarningSounds = AccessTools.Method(typeof(Survival), "UpdateWarningSounds", new Type[5]
	{
		typeof(PDANotification[]),
		typeof(float),
		typeof(float),
		typeof(float),
		typeof(float)
	}, (Type[])null);

	private static bool s_logged;

	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, Survival __instance, float timePassed, ref float __result)
	{
		if (__exception == null || (Object)(object)__instance == (Object)null)
		{
			return null;
		}
		if (!s_logged)
		{
			s_logged = true;
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("Survival.UpdateStats failed in another mod (" + __exception.GetType().Name + ": " + __exception.Message + "). InventoryStacking will apply vanilla food/water decay so hunger and thirst bars keep working."));
			}
		}
		__result = ApplyVanillaDecay(__instance, timePassed);
		return null;
	}

	private static float ApplyVanillaDecay(Survival survival, float timePassed)
	{
		float num = 0f;
		if (timePassed <= float.Epsilon)
		{
			return num;
		}
		float food = survival.food;
		float water = survival.water;
		float num2 = timePassed / 2520f * 100f;
		if (num2 > survival.food)
		{
			num += (num2 - survival.food) * 25f;
		}
		survival.food = Mathf.Clamp(survival.food - num2, 0f, 200f);
		float num3 = timePassed / 1800f * 100f;
		if (num3 > survival.water)
		{
			num += (num3 - survival.water) * 25f;
		}
		survival.water = Mathf.Clamp(survival.water - num3, 0f, 100f);
		TryWarningSounds(survival, food, water);
		return num;
	}

	private static void TryWarningSounds(Survival survival, float prevFood, float prevWater)
	{
		if (UpdateWarningSounds == null)
		{
			return;
		}
		try
		{
			UpdateWarningSounds.Invoke(survival, new object[5] { survival.foodWarningSounds, survival.food, prevFood, 20f, 10f });
			UpdateWarningSounds.Invoke(survival, new object[5] { survival.waterWarningSounds, survival.water, prevWater, 20f, 10f });
		}
		catch (Exception ex)
		{
			if (!s_logged)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					log.LogDebug((object)("Survival warning sounds skipped: " + ex.Message));
				}
			}
		}
	}
}
