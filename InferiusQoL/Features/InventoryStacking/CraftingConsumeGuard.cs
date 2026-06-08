#nullable disable
using UnityEngine;

namespace InferiusQoL.Features.InventoryStacking;

internal static class CraftingConsumeGuard
{
	private static int _frame = -1;

	private static string _owner;

	public static bool TryClaim(string owner)
	{
		int frameCount = Time.frameCount;
		if (_frame != frameCount || _owner == owner)
		{
			_frame = frameCount;
			_owner = owner;
			return true;
		}
		return false;
	}
}
