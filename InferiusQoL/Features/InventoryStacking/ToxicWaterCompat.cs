#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class ToxicWaterCompat
{
	private const string ToxicWaterTypeName = "ToxicWaterMod.ToxicWater";

	private static Type _toxicWaterType;

	private static MethodInfo _activateSerumMethod;

	private static MethodInfo _saveActiveSerumStateMethod;

	private static FieldInfo _serumUseCooldownTimerField;

	internal static bool IsAvailable => ToxicWaterType != null;

	private static Type ToxicWaterType
	{
		get
		{
			if (_toxicWaterType == null)
			{
				_toxicWaterType = AccessTools.TypeByName("ToxicWaterMod.ToxicWater");
			}
			return _toxicWaterType;
		}
	}

	internal static MethodInfo UseSerumPickupMethod
	{
		get
		{
			Type toxicWaterType = ToxicWaterType;
			if (toxicWaterType == null)
			{
				return null;
			}
			Type type = AccessTools.Inner(toxicWaterType, "SerumTier");
			if (!(type == null))
			{
				return AccessTools.Method(toxicWaterType, "UseSerumPickup", new Type[4]
				{
					typeof(Pickupable),
					typeof(float),
					typeof(float),
					type
				}, (Type[])null);
			}
			return null;
		}
	}

	internal static int CountSerumStackUnits(ItemsContainer container, TechType targetType)
	{
		if (container == null || (int)targetType == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			if (!((Object)(object)((item != null) ? item.item : null) == (Object)null) && ResolvePickupTechType(item.item) == targetType)
			{
				num += ((!StackRules.CanStack(item.item)) ? 1 : MRStack.CountOf(item.item));
			}
		}
		return num;
	}

	internal static void ConsumeOneSerumUnit(object toxicWater, Pickupable pickupable, float duration, float cleanPercent, object tier)
	{
		if (toxicWater != null && !((Object)(object)pickupable == (Object)null))
		{
			EnsureReflection();
			_activateSerumMethod?.Invoke(toxicWater, new object[3] { duration, cleanPercent, tier });
			MRStack.Add(pickupable, -1);
			if (_serumUseCooldownTimerField != null)
			{
				_serumUseCooldownTimerField.SetValue(toxicWater, 5f);
			}
			_saveActiveSerumStateMethod?.Invoke(toxicWater, null);
			CraftingCounts.InvalidateCache();
			StackIconRefresher.Trigger();
		}
	}

	private static void EnsureReflection()
	{
		Type toxicWaterType = ToxicWaterType;
		if (toxicWaterType == null)
		{
			return;
		}
		if (_activateSerumMethod == null)
		{
			Type type = AccessTools.Inner(toxicWaterType, "SerumTier");
			if (type != null)
			{
				_activateSerumMethod = AccessTools.Method(toxicWaterType, "ActivateSerum", new Type[3]
				{
					typeof(float),
					typeof(float),
					type
				}, (Type[])null);
			}
		}
		if (_saveActiveSerumStateMethod == null)
		{
			_saveActiveSerumStateMethod = AccessTools.Method(toxicWaterType, "SaveActiveSerumState", (Type[])null, (Type[])null);
		}
		if (_serumUseCooldownTimerField == null)
		{
			_serumUseCooldownTimerField = AccessTools.Field(toxicWaterType, "serumUseCooldownTimer");
		}
	}

	private static TechType ResolvePickupTechType(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null)
		{
			return (TechType)0;
		}
		TechType result = pickupable.GetTechType();
		TechTag component = ((Component)pickupable).GetComponent<TechTag>();
		if ((Object)(object)component != (Object)null && (int)component.type != 0)
		{
			result = component.type;
		}
		return result;
	}
}
