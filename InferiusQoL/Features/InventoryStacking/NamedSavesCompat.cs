#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace InferiusQoL.Features.InventoryStacking;

internal static class NamedSavesCompat
{
	private static Dictionary<string, string> s_cache;

	private static long s_cacheWriteTicks = -1L;

	private static string ConfigPath => Path.Combine(Paths.ConfigPath, "NamedSaves", "config.json");

	public static bool IsAvailable => File.Exists(ConfigPath);

	public static string TryGetCustomName(string saveSlotId)
	{
		if (string.IsNullOrEmpty(saveSlotId) || !IsAvailable)
		{
			return null;
		}
		try
		{
			long ticks = File.GetLastWriteTimeUtc(ConfigPath).Ticks;
			if (s_cache == null || ticks != s_cacheWriteTicks)
			{
				s_cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(ConfigPath)) ?? new Dictionary<string, string>(StringComparer.Ordinal);
				s_cacheWriteTicks = ticks;
			}
			if (s_cache.TryGetValue(saveSlotId, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("NamedSavesCompat read failed: " + ex.Message));
			}
		}
		return null;
	}

	public static string FormatSaveLabel(string saveSlotId, int saveMenuIndex = 0, string saveDisplayKey = null)
	{
		if (!string.IsNullOrEmpty(saveDisplayKey))
		{
			string text = TryGetCustomName(saveDisplayKey);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		string text2 = TryGetCustomName(saveSlotId);
		if (!string.IsNullOrEmpty(text2))
		{
			return text2;
		}
		return SaveSlotMetadata.FormatVanillaSlotLabel(saveSlotId, saveMenuIndex);
	}
}
