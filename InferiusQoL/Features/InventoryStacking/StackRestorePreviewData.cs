#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackRestorePreviewData
{
	private sealed class SaveDataDto
	{
		public Dictionary<string, int> bySlotKey = new Dictionary<string, int>(StringComparer.Ordinal);

		public Dictionary<string, List<int>> byPlayerTechStacks = new Dictionary<string, List<int>>(StringComparer.Ordinal);
	}

	public static int CountPlayerInventoryUnits(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return 0;
		}
		try
		{
			SaveDataDto saveDataDto = JsonConvert.DeserializeObject<SaveDataDto>(File.ReadAllText(filePath));
			if (saveDataDto == null)
			{
				return 0;
			}
			int num = 0;
			if (saveDataDto.bySlotKey != null)
			{
				foreach (KeyValuePair<string, int> item in saveDataDto.bySlotKey)
				{
					if (item.Value >= 1 && !string.IsNullOrEmpty(item.Key) && item.Key.StartsWith("player|", StringComparison.Ordinal))
					{
						num += item.Value;
					}
				}
			}
			if (num > 0)
			{
				return num;
			}
			if (saveDataDto.byPlayerTechStacks != null)
			{
				foreach (KeyValuePair<string, List<int>> byPlayerTechStack in saveDataDto.byPlayerTechStacks)
				{
					if (byPlayerTechStack.Value != null)
					{
						for (int i = 0; i < byPlayerTechStack.Value.Count; i++)
						{
							num += byPlayerTechStack.Value[i];
						}
					}
				}
			}
			return num;
		}
		catch
		{
			return 0;
		}
	}

	public static bool TryLoadPlayerFirstPage(string filePath, out StackRestorePlayerPage page)
	{
		page = new StackRestorePlayerPage();
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return false;
		}
		try
		{
			SaveDataDto saveDataDto = JsonConvert.DeserializeObject<SaveDataDto>(File.ReadAllText(filePath));
			if (saveDataDto?.bySlotKey == null)
			{
				return true;
			}
			foreach (KeyValuePair<string, int> item in saveDataDto.bySlotKey)
			{
				if (item.Value >= 1 && TryParsePlayerSlotKey(item.Key, out var techId, out var x, out var y) && x >= 0 && x < 6 && y >= 0 && y < 4)
				{
					page.Set(x, y, (TechType)techId, item.Value);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("StackRestorePreviewData load failed: " + ex.Message));
			}
			return false;
		}
	}

	private static bool TryParsePlayerSlotKey(string key, out int techId, out int x, out int y)
	{
		techId = 0;
		x = 0;
		y = 0;
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		string[] array = key.Split('|');
		if (array.Length < 4 || !string.Equals(array[0], "player", StringComparison.Ordinal))
		{
			return false;
		}
		if (int.TryParse(array[1], out techId) && int.TryParse(array[2], out x))
		{
			return int.TryParse(array[3], out y);
		}
		return false;
	}
}
