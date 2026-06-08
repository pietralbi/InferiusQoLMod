#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

using UnityEngine;
using Object = UnityEngine.Object;

internal static class InventoryStackingUnity
{
	public static void SetIsKinematicAndUpdateInterpolation(Rigidbody body, bool isKinematic, bool interpolate)
	{
		if ((Object)(object)body == (Object)null)
			return;

		body.isKinematic = isKinematic;
		body.interpolation = interpolate
			? RigidbodyInterpolation.Interpolate
			: RigidbodyInterpolation.None;
	}
}
