#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackBackupRing
{
	public sealed class BackupInfo
	{
		public int SlotIndex;

		public DateTime SavedAtUtc;

		public int InventoryItemCount;

		public string FilePath;

		public string SaveSlotId;

		public string SaveDisplayKey;

		public int SaveMenuIndex;

		public DateTime? SaveSlotTimeUtc;

		public string GetSaveLabel()
		{
			if (string.IsNullOrEmpty(SaveSlotId))
			{
				return "Unknown save";
			}
			int saveMenuIndex = SaveSlotMetadata.TryResolveMenuIndex(SaveSlotId, SaveMenuIndex);
			return NamedSavesCompat.FormatSaveLabel(SaveSlotId, saveMenuIndex, SaveDisplayKey);
		}

		public string FormatSaveSlotTimeLocal()
		{
			if (!SaveSlotTimeUtc.HasValue)
			{
				return null;
			}
			return SaveSlotTimeUtc.Value.ToLocalTime().ToString("g");
		}

		public string FormatBackupTimeLocal()
		{
			return SavedAtUtc.ToLocalTime().ToString("g");
		}
	}

	private sealed class SlotEntry
	{
		public bool present;

		public DateTime savedAtUtc;

		public int inventoryItemCount;

		public string saveSlotId;

		public string saveDisplayKey;

		public int saveMenuIndex;

		public DateTime? saveSlotTimeUtc;
	}

	private sealed class Manifest
	{
		public int version = 2;

		public int nextWriteSlot;

		public SlotEntry[] slots = new SlotEntry[100];
	}

	public const int MaxBackups = 100;

	private const int ManifestVersion = 2;

	private static string BackupsDirectory => InventoryStackingPaths.BackupsDirectory;

	private static string ManifestPath => InventoryStackingPaths.BackupManifestPath;

	private static string LegacyBackupPath => InventoryStackingPaths.LegacyBackupPath;

	public static bool HasAnyBackup()
	{
		TryMigrateLegacyBackup();
		EnsureSlotFileNames();
		Manifest manifest = LoadManifest();
		for (int i = 0; i < 100; i++)
		{
			if (IsSlotPresent(manifest, i))
			{
				return true;
			}
		}
		return false;
	}

	public static IReadOnlyList<BackupInfo> GetBackupsOldestFirst()
	{
		TryMigrateLegacyBackup();
		EnsureSlotFileNames();
		Manifest manifest = LoadManifest();
		List<BackupInfo> list = new List<BackupInfo>(100);
		for (int i = 0; i < 100; i++)
		{
			if (IsSlotPresent(manifest, i))
			{
				SlotEntry slotEntry = manifest.slots[i];
				list.Add(new BackupInfo
				{
					SlotIndex = i,
					SavedAtUtc = slotEntry.savedAtUtc,
					InventoryItemCount = slotEntry.inventoryItemCount,
					FilePath = GetSlotFilePath(i),
					SaveSlotId = slotEntry.saveSlotId,
					SaveDisplayKey = slotEntry.saveDisplayKey,
					SaveMenuIndex = slotEntry.saveMenuIndex,
					SaveSlotTimeUtc = slotEntry.saveSlotTimeUtc
				});
			}
		}
		list.Sort(delegate(BackupInfo a, BackupInfo b)
		{
			int num = a.SavedAtUtc.CompareTo(b.SavedAtUtc);
			return (num == 0) ? a.SlotIndex.CompareTo(b.SlotIndex) : num;
		});
		return list;
	}

	public static void WriteBackup(string json, int inventoryItemCount, string saveSlotId, string saveDisplayKey, int saveMenuIndex, DateTime? saveSlotTimeUtc)
	{
		if (!string.IsNullOrEmpty(json))
		{
			TryMigrateLegacyBackup();
			EnsureSlotFileNames();
			Directory.CreateDirectory(BackupsDirectory);
			Manifest manifest = LoadManifest();
			int num = manifest.nextWriteSlot % 100;
			File.WriteAllText(GetSlotFilePath(num), json);
			if (manifest.slots[num] == null)
			{
				manifest.slots[num] = new SlotEntry();
			}
			manifest.slots[num].present = true;
			manifest.slots[num].savedAtUtc = DateTime.UtcNow;
			manifest.slots[num].inventoryItemCount = Math.Max(0, inventoryItemCount);
			manifest.slots[num].saveSlotId = saveSlotId;
			manifest.slots[num].saveDisplayKey = saveDisplayKey;
			manifest.slots[num].saveMenuIndex = Math.Max(0, saveMenuIndex);
			manifest.slots[num].saveSlotTimeUtc = saveSlotTimeUtc;
			manifest.nextWriteSlot = (num + 1) % 100;
			manifest.version = 2;
			SaveManifest(manifest);
		}
	}

	public static string GetNewestBackupPath()
	{
		IReadOnlyList<BackupInfo> backupsOldestFirst = GetBackupsOldestFirst();
		if (backupsOldestFirst.Count == 0)
		{
			return null;
		}
		return backupsOldestFirst[backupsOldestFirst.Count - 1].FilePath;
	}

	private static bool IsSlotPresent(Manifest manifest, int slot)
	{
		if (manifest?.slots == null || slot < 0 || slot >= 100)
		{
			return false;
		}
		SlotEntry slotEntry = manifest.slots[slot];
		if (slotEntry != null && slotEntry.present)
		{
			return File.Exists(GetSlotFilePath(slot));
		}
		return false;
	}

	private static string GetSlotFilePath(int slot)
	{
		return Path.Combine(BackupsDirectory, $"slot-{slot:D3}.json");
	}

	private static void EnsureSlotFileNames()
	{
		if (!Directory.Exists(BackupsDirectory))
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			string text = Path.Combine(BackupsDirectory, $"slot-{i:D2}.json");
			string slotFilePath = GetSlotFilePath(i);
			if (!File.Exists(text) || File.Exists(slotFilePath))
			{
				continue;
			}
			try
			{
				File.Move(text, slotFilePath);
			}
			catch (Exception ex)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					log.LogWarning((object)("StackBackupRing slot rename failed: " + ex.Message));
				}
			}
		}
	}

	private static Manifest LoadManifest()
	{
		if (!File.Exists(ManifestPath))
		{
			return NewManifest();
		}
		try
		{
			return NormalizeManifest(JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(ManifestPath)));
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("StackBackupRing manifest load failed: " + ex.Message));
			}
			return NewManifest();
		}
	}

	private static Manifest NewManifest()
	{
		return new Manifest
		{
			version = 2,
			nextWriteSlot = 0,
			slots = new SlotEntry[100]
		};
	}

	private static Manifest NormalizeManifest(Manifest manifest)
	{
		Manifest manifest2 = NewManifest();
		if (manifest?.slots != null)
		{
			int num = Math.Min(manifest.slots.Length, 100);
			for (int i = 0; i < num; i++)
			{
				manifest2.slots[i] = manifest.slots[i];
			}
		}
		manifest2.nextWriteSlot = manifest?.nextWriteSlot ?? 0;
		if (manifest2.nextWriteSlot < 0 || manifest2.nextWriteSlot >= 100)
		{
			manifest2.nextWriteSlot = 0;
		}
		manifest2.version = 2;
		return manifest2;
	}

	private static void SaveManifest(Manifest manifest)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath) ?? Paths.ConfigPath);
		manifest.version = 2;
		File.WriteAllText(ManifestPath, JsonConvert.SerializeObject((object)manifest, (Formatting)1));
	}

	private static void TryMigrateLegacyBackup()
	{
		if (!File.Exists(LegacyBackupPath))
		{
			return;
		}
		Manifest manifest = LoadManifest();
		for (int i = 0; i < 100; i++)
		{
			if (IsSlotPresent(manifest, i))
			{
				return;
			}
		}
		try
		{
			Directory.CreateDirectory(BackupsDirectory);
			string slotFilePath = GetSlotFilePath(0);
			File.Copy(LegacyBackupPath, slotFilePath, overwrite: true);
			manifest.slots[0] = new SlotEntry
			{
				present = true,
				savedAtUtc = File.GetLastWriteTimeUtc(LegacyBackupPath),
				inventoryItemCount = StackRestorePreviewData.CountPlayerInventoryUnits(slotFilePath)
			};
			manifest.nextWriteSlot = 1;
			SaveManifest(manifest);
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogInfo((object)"StackBackupRing migrated legacy single backup into slot 0.");
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = Plugin.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("StackBackupRing legacy migration failed: " + ex.Message));
			}
		}
	}
}
