#nullable disable
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackQuality
{
	public static void CopyForSplit(Pickupable source, Pickupable target)
	{
		if ((Object)(object)source == (Object)(object)target)
		{
			return;
		}

		Eatable sourceEatable = GetEatable(source);
		Eatable targetEatable = GetEatable(target);
		if ((Object)(object)sourceEatable == (Object)null || (Object)(object)targetEatable == (Object)null)
		{
			return;
		}

		targetEatable.allowOverfill = sourceEatable.allowOverfill;
		targetEatable.despawns = sourceEatable.despawns;
		targetEatable.despawnDelay = sourceEatable.despawnDelay;
		targetEatable.foodValue = sourceEatable.foodValue;
		targetEatable.waterValue = sourceEatable.waterValue;
		targetEatable.stomachVolume = sourceEatable.stomachVolume;
		targetEatable.kDecayRate = sourceEatable.kDecayRate;
		targetEatable.SetDecomposes(sourceEatable.decomposes);
		targetEatable.timeDecayStart = sourceEatable.timeDecayStart;
	}

	public static bool CanMerge(Pickupable a, Pickupable b)
	{
		if (!StackFlareState.CanMerge(a, b))
		{
			return false;
		}

		Eatable eatableA = GetEatable(a);
		Eatable eatableB = GetEatable(b);
		if ((Object)(object)eatableA == (Object)null || (Object)(object)eatableB == (Object)null)
		{
			return true;
		}

		return eatableA.IsRotten() == eatableB.IsRotten();
	}

	public static void AverageInto(Pickupable survivor, int survivorUnits, Pickupable incoming, int incomingUnits)
	{
		Eatable survivorEatable = GetEatable(survivor);
		Eatable incomingEatable = GetEatable(incoming);
		if ((Object)(object)survivorEatable == (Object)null || (Object)(object)incomingEatable == (Object)null)
		{
			return;
		}

		survivorUnits = Mathf.Max(0, survivorUnits);
		incomingUnits = Mathf.Max(0, incomingUnits);
		int totalUnits = survivorUnits + incomingUnits;
		if (totalUnits <= 0)
		{
			return;
		}

		float rate = survivorEatable.kDecayRate;
		if (rate <= float.Epsilon)
		{
			rate = incomingEatable.kDecayRate;
			survivorEatable.kDecayRate = rate;
		}
		if (rate <= float.Epsilon)
		{
			return;
		}

		float now = GetTimePassed();
		float survivorDecay = GetEffectiveDecayValue(survivorEatable, now);
		float incomingDecay = GetEffectiveDecayValue(incomingEatable, now);
		bool decomposes = survivorEatable.decomposes || incomingEatable.decomposes;
		survivorEatable.SetDecomposes(decomposes);
		if (!decomposes)
		{
			return;
		}

		float averageDecay = ((survivorDecay * survivorUnits) + (incomingDecay * incomingUnits)) / totalUnits;
		survivorEatable.timeDecayStart = now - (averageDecay / rate);
	}

	private static Eatable GetEatable(Pickupable pickupable)
	{
		if ((Object)(object)pickupable == (Object)null)
		{
			return null;
		}

		return ((Component)pickupable).GetComponentInChildren<Eatable>(true);
	}

	private static float GetEffectiveDecayValue(Eatable eatable, float now)
	{
		if ((Object)(object)eatable == (Object)null || !eatable.decomposes || eatable.kDecayRate <= float.Epsilon)
		{
			return 0f;
		}

		float elapsed = Mathf.Max(0f, now - eatable.timeDecayStart);
		float rawDecay = elapsed * eatable.kDecayRate;
		float foodDecay = eatable.foodValue - Mathf.Max(eatable.foodValue - rawDecay, -25f);
		float waterDecay = eatable.waterValue - Mathf.Max(eatable.waterValue - rawDecay, -25f);
		return Mathf.Max(0f, foodDecay, waterDecay);
	}

	private static float GetTimePassed()
	{
		return DayNightCycle.main != null ? DayNightCycle.main.timePassedAsFloat : Time.time;
	}
}
