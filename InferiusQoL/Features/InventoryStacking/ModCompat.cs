#nullable disable
using BepInEx.Bootstrap;

namespace InferiusQoL.Features.InventoryStacking;

internal static class ModCompat
{
	internal const string DeathrunRemadePluginGuid = "com.github.tinyhoot.DeathrunRemade";

	internal static bool IsDeathrunRemadePresent
	{
		get
		{
			if (Chainloader.PluginInfos != null)
			{
				return Chainloader.PluginInfos.ContainsKey("com.github.tinyhoot.DeathrunRemade");
			}
			return false;
		}
	}
}
