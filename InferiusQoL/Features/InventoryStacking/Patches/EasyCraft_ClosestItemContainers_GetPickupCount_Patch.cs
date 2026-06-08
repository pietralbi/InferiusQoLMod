#nullable disable
using System;
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch]
internal static class EasyCraft_ClosestItemContainers_GetPickupCount_Patch
{
	public static bool Prepare()
	{
		Type containersType = EasyCraftCompat.ContainersType;
		if (containersType == null)
		{
			return false;
		}
		return AccessTools.Method(containersType, "GetPickupCount", new Type[1] { typeof(TechType) }, (Type[])null) != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase Target()
	{
		return AccessTools.Method(EasyCraftCompat.ContainersType, "GetPickupCount", new Type[1] { typeof(TechType) }, (Type[])null);
	}

	[HarmonyPrefix]
	[HarmonyPriority(0)]
	private static bool Prefix(TechType techType, ref int __result)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		ItemsContainer[] containers = EasyCraftCompat.GetContainers();
		if (containers == null || containers.Length == 0)
		{
			__result = 0;
			return false;
		}
		int num = 0;
		foreach (ItemsContainer val in containers)
		{
			if (val != null)
			{
				num += MRStack.TotalStackUnits(val, techType);
			}
		}
		__result = num;
		return false;
	}
}
