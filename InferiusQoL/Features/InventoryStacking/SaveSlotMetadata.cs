#nullable disable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class SaveSlotMetadata
{
	private static string s_cachedSaveSlotId;

	private static string s_cachedSaveDisplayKey;

	private static int s_cachedMenuIndex;

	internal static void CacheCurrentSlot()
	{
		if (TryReadCurrent(out var saveSlotId, out var saveDisplayKey, out var saveMenuIndex, out var _))
		{
			s_cachedSaveSlotId = saveSlotId;
			s_cachedSaveDisplayKey = saveDisplayKey;
			s_cachedMenuIndex = saveMenuIndex;
		}
	}

	public static void TryCaptureCurrent(out string saveSlotId, out string saveDisplayKey, out DateTime? saveSlotTimeUtc, out int saveMenuIndex)
	{
		saveSlotId = null;
		saveDisplayKey = null;
		saveSlotTimeUtc = null;
		saveMenuIndex = 0;
		if (TryReadCurrent(out var saveSlotId2, out var saveDisplayKey2, out var saveMenuIndex2, out var saveSlotTimeUtc2))
		{
			saveSlotId = saveSlotId2;
			saveDisplayKey = saveDisplayKey2;
			saveMenuIndex = saveMenuIndex2;
			saveSlotTimeUtc = saveSlotTimeUtc2;
			s_cachedSaveSlotId = saveSlotId2;
			s_cachedSaveDisplayKey = saveDisplayKey2;
			s_cachedMenuIndex = saveMenuIndex2;
		}
		else if (!string.IsNullOrEmpty(s_cachedSaveSlotId))
		{
			saveSlotId = s_cachedSaveSlotId;
			saveDisplayKey = s_cachedSaveDisplayKey;
			saveMenuIndex = s_cachedMenuIndex;
			saveSlotTimeUtc = TryReadSlotTime(s_cachedSaveSlotId);
		}
	}

	public static int TryResolveMenuIndex(string saveSlotId, int storedMenuIndex)
	{
		if (!string.IsNullOrEmpty(saveSlotId) && (Object)(object)SaveLoadManager.main != (Object)null)
		{
			int num = ResolveMenuIndex(saveSlotId);
			if (num > 0)
			{
				return num;
			}
		}
		if (storedMenuIndex <= 0)
		{
			return 0;
		}
		return storedMenuIndex;
	}

	public static string FormatVanillaSlotLabel(string saveSlotId, int saveMenuIndex)
	{
		if (saveMenuIndex > 0)
		{
			return $"Slot {saveMenuIndex}";
		}
		if (!string.IsNullOrEmpty(saveSlotId))
		{
			return saveSlotId;
		}
		return "Unknown save";
	}

	internal static string BuildSaveDisplayKey(string saveSlotId)
	{
		if ((Object)(object)SaveLoadManager.main == (Object)null || string.IsNullOrEmpty(saveSlotId))
		{
			return null;
		}
		SaveLoadManager.GameInfo gameInfo = SaveLoadManager.main.GetGameInfo(saveSlotId);
		if (gameInfo == null || gameInfo.dateTicks <= 0)
		{
			return null;
		}
		return "Saved" + gameInfo.dateTicks;
	}

	private static bool TryReadCurrent(out string saveSlotId, out string saveDisplayKey, out int saveMenuIndex, out DateTime? saveSlotTimeUtc)
	{
		saveSlotId = null;
		saveDisplayKey = null;
		saveMenuIndex = 0;
		saveSlotTimeUtc = null;
		if ((Object)(object)SaveLoadManager.main == (Object)null)
		{
			return false;
		}
		string currentSlot = SaveLoadManager.main.GetCurrentSlot();
		if (string.IsNullOrEmpty(currentSlot))
		{
			return false;
		}
		saveSlotId = NormalizeSlotId(currentSlot);
		saveDisplayKey = BuildSaveDisplayKey(saveSlotId);
		saveMenuIndex = ResolveMenuIndex(saveSlotId);
		saveSlotTimeUtc = TryReadSlotTime(saveSlotId);
		return true;
	}

	private static string NormalizeSlotId(string slot)
	{
		if (string.IsNullOrEmpty(slot))
		{
			return slot;
		}
		int num = slot.LastIndexOf('/');
		if (num >= 0 && num < slot.Length - 1)
		{
			slot = slot.Substring(num + 1);
		}
		num = slot.LastIndexOf('\\');
		if (num >= 0 && num < slot.Length - 1)
		{
			slot = slot.Substring(num + 1);
		}
		return slot;
	}

	private static int ResolveMenuIndex(string saveSlotId)
	{
		if ((Object)(object)SaveLoadManager.main == (Object)null || string.IsNullOrEmpty(saveSlotId))
		{
			return 0;
		}
		string[] activeSlotNames = SaveLoadManager.main.GetActiveSlotNames();
		if (activeSlotNames == null || activeSlotNames.Length == 0)
		{
			return 0;
		}
		Array.Sort(activeSlotNames, CompareSlotsNewestFirst);
		for (int i = 0; i < activeSlotNames.Length; i++)
		{
			if (string.Equals(activeSlotNames[i], saveSlotId, StringComparison.OrdinalIgnoreCase))
			{
				return i + 1;
			}
		}
		return 0;
	}

	private static int CompareSlotsNewestFirst(string a, string b)
	{
		long dateTicks = GetDateTicks(a);
		int num = GetDateTicks(b).CompareTo(dateTicks);
		if (num != 0)
		{
			return num;
		}
		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
	}

	private static long GetDateTicks(string saveSlotId)
	{
		SaveLoadManager main = SaveLoadManager.main;
		return ((main != null) ? main.GetGameInfo(saveSlotId) : null)?.dateTicks ?? 0;
	}

	private static DateTime? TryReadSlotTime(string saveSlotId)
	{
		if ((Object)(object)SaveLoadManager.main == (Object)null || string.IsNullOrEmpty(saveSlotId))
		{
			return null;
		}
		SaveLoadManager.GameInfo gameInfo = SaveLoadManager.main.GetGameInfo(saveSlotId);
		if (gameInfo == null || gameInfo.dateTicks <= 0)
		{
			return null;
		}
		try
		{
			return new DateTime(gameInfo.dateTicks, DateTimeKind.Local).ToUniversalTime();
		}
		catch
		{
			return null;
		}
	}
}
