#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackUidPersistence
{
	private sealed class SaveData
	{
		public Dictionary<string, int> byPickupUid = new Dictionary<string, int>(StringComparer.Ordinal);

		public Dictionary<string, int> bySlotKey = new Dictionary<string, int>(StringComparer.Ordinal);

		public Dictionary<string, List<int>> byPlayerTechStacks = new Dictionary<string, List<int>>(StringComparer.Ordinal);
	}

	private static readonly Dictionary<string, int> s_loadedUid = new Dictionary<string, int>(StringComparer.Ordinal);

	private static readonly Dictionary<string, int> s_loadedSlot = new Dictionary<string, int>(StringComparer.Ordinal);

	private static readonly Dictionary<int, List<int>> s_restorePlayerTechStacks = new Dictionary<int, List<int>>();

	private static bool s_isLoaded;

	private static readonly HashSet<int> s_sidecarConsumedPickupIds = new HashSet<int>();

	private static readonly HashSet<ItemsContainer> s_seamothSidecarRetryScheduled = new HashSet<ItemsContainer>();

	public static void MarkUnloaded()
	{
		s_isLoaded = false;
		s_loadedUid.Clear();
		s_loadedSlot.Clear();
		s_restorePlayerTechStacks.Clear();
		s_sidecarConsumedPickupIds.Clear();
		s_seamothSidecarRetryScheduled.Clear();
	}

	public static void LoadForCurrentSession()
	{
		if (s_isLoaded)
		{
			return;
		}
		s_isLoaded = true;
		try
		{
			string path = GetPath();
			if (File.Exists(path))
			{
				SaveData saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));
				if (saveData != null)
				{
					s_loadedUid.Clear();
					s_loadedSlot.Clear();
					MergeDict(saveData.byPickupUid, s_loadedUid);
					MergeDict(saveData.bySlotKey, s_loadedSlot);
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("StackUidPersistence load failed: " + ex.Message));
			}
		}
	}

	private static void MergeDict(Dictionary<string, int> src, Dictionary<string, int> dst)
	{
		if (src == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item in src)
		{
			if (!string.IsNullOrEmpty(item.Key) && item.Value > 1)
			{
				dst[item.Key] = item.Value;
			}
		}
	}

	private static void MergePlayerTechStacks(Dictionary<string, List<int>> src, Dictionary<int, List<int>> dst)
	{
		if (src == null)
		{
			return;
		}
		foreach (KeyValuePair<string, List<int>> item in src)
		{
			if (string.IsNullOrEmpty(item.Key) || item.Value == null || !int.TryParse(item.Key, out var result))
			{
				continue;
			}
			if (!dst.TryGetValue(result, out var value))
			{
				value = (dst[result] = new List<int>());
			}
			for (int i = 0; i < item.Value.Count; i++)
			{
				int num = item.Value[i];
				if (num > 1)
				{
					value.Add(num);
				}
			}
			if (value.Count > 1)
			{
				value.Sort((int a, int b) => b.CompareTo(a));
			}
		}
	}

	public static void SaveCurrentSession()
	{
		try
		{
			SaveData data = LoadSaveDataFromDiskNormalized();
			data.byPlayerTechStacks.Clear();
			HashSet<ItemsContainer> capturedContainers = new HashSet<ItemsContainer>();
			if ((Object)(object)Inventory.main != (Object)null)
			{
				CaptureContainer(Inventory.main.container);
				CapturePlayerTechStacks(data, Inventory.main.container);
				int usedStorageCount = Inventory.main.GetUsedStorageCount();
				for (int i = 0; i < usedStorageCount; i++)
				{
					IItemsContainer usedStorage = Inventory.main.GetUsedStorage(i);
					ItemsContainer val = (ItemsContainer)(object)((usedStorage is ItemsContainer) ? usedStorage : null);
					if (val != null)
					{
						CaptureContainer(val);
					}
				}
			}
			CaptureWorldStorageContainers(data, capturedContainers);
			Inventory main = Inventory.main;
			int inventoryItemCount = CountPlayerInventoryUnits((main != null) ? main.container : null);
			int num = data.byPickupUid.Count + data.bySlotKey.Count;
			if (num == 0 && (Object)(object)Inventory.main == (Object)null)
			{
				return;
			}
			WriteSaveDataToDisk(data, inventoryItemCount, rotateBackup: true);
			if (num <= 0)
			{
				return;
			}
			int num2 = 0;
			foreach (KeyValuePair<string, List<int>> byPlayerTechStack in data.byPlayerTechStacks)
			{
				if (byPlayerTechStack.Value != null)
				{
					num2 += byPlayerTechStack.Value.Count;
				}
			}
			string path = GetPath();
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogInfo((object)$"StackUidPersistence saved {num} stack entries ({num2} player-tech) to {path}");
			}
			void CaptureContainer(ItemsContainer c)
			{
				if (c == null || !capturedContainers.Add(c))
				{
					return;
				}
				foreach (InventoryItem item in (IEnumerable<InventoryItem>)c)
				{
					CaptureOne(data, c, item);
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = Plugin.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("StackUidPersistence save failed: " + ex.Message));
			}
		}
	}

	internal static void MergeCaptureContainerIntoSidecarFile(ItemsContainer container)
	{
		if (container == null)
		{
			return;
		}
		try
		{
			SaveData data = LoadSaveDataFromDiskNormalized();
			foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
			{
				CaptureOne(data, container, item);
			}
			Inventory main = Inventory.main;
			WriteSaveDataToDisk(data, CountPlayerInventoryUnits((main != null) ? main.container : null), rotateBackup: false);
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("MergeCaptureContainerIntoSidecarFile failed: " + ex.Message));
			}
		}
	}

	private static SaveData LoadSaveDataFromDiskNormalized()
	{
		string path = GetPath();
		if (!File.Exists(path))
		{
			return NewEmptySaveData();
		}
		try
		{
			SaveData saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));
			if (saveData == null)
			{
				return NewEmptySaveData();
			}
			if (saveData.byPickupUid == null)
			{
				saveData.byPickupUid = new Dictionary<string, int>(StringComparer.Ordinal);
			}
			if (saveData.bySlotKey == null)
			{
				saveData.bySlotKey = new Dictionary<string, int>(StringComparer.Ordinal);
			}
			if (saveData.byPlayerTechStacks == null)
			{
				saveData.byPlayerTechStacks = new Dictionary<string, List<int>>(StringComparer.Ordinal);
			}
			return saveData;
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("StackUidPersistence: could not read sidecar, starting fresh: " + ex.Message));
			}
			return NewEmptySaveData();
		}
	}

	private static SaveData NewEmptySaveData()
	{
		return new SaveData
		{
			byPickupUid = new Dictionary<string, int>(StringComparer.Ordinal),
			bySlotKey = new Dictionary<string, int>(StringComparer.Ordinal),
			byPlayerTechStacks = new Dictionary<string, List<int>>(StringComparer.Ordinal)
		};
	}

	private static int CountPlayerInventoryUnits(ItemsContainer pocket)
	{
		if (pocket == null)
		{
			return 0;
		}
		int num = 0;
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)pocket)
		{
			Pickupable val = ((item != null) ? item.item : null);
			if ((Object)(object)val != (Object)null)
			{
				num += MRStack.CountOf(val);
			}
		}
		return num;
	}

	private static void WriteSaveDataToDisk(SaveData data, int inventoryItemCount, bool rotateBackup)
	{
		string path = GetPath();
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Paths.ConfigPath);
		string text = JsonConvert.SerializeObject((object)data, (Formatting)0);
		File.WriteAllText(path, text);
		if (rotateBackup)
		{
			SaveSlotMetadata.TryCaptureCurrent(out var saveSlotId, out var saveDisplayKey, out var saveSlotTimeUtc, out var saveMenuIndex);
			WriteBackupCopy(text, inventoryItemCount, saveSlotId, saveDisplayKey, saveMenuIndex, saveSlotTimeUtc);
		}
	}

	private static void WriteBackupCopy(string json, int inventoryItemCount, string saveSlotId, string saveDisplayKey, int saveMenuIndex, DateTime? saveSlotTimeUtc)
	{
		try
		{
			StackBackupRing.WriteBackup(json, inventoryItemCount, saveSlotId, saveDisplayKey, saveMenuIndex, saveSlotTimeUtc);
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("StackUidPersistence backup write failed: " + ex.Message));
			}
		}
	}

	public static bool BackupExists()
	{
		return StackBackupRing.HasAnyBackup();
	}

	public static string GetSidecarPath()
	{
		return GetPath();
	}

	public static string GetBackupSidecarPath()
	{
		return StackBackupRing.GetNewestBackupPath();
	}

	public static bool TryRestoreFromBackup(string backupFilePath, out string userMessage)
	{
		userMessage = null;
		if (string.IsNullOrEmpty(backupFilePath) || !File.Exists(backupFilePath))
		{
			userMessage = "No stack backup file found. Play and save once to create a backup.";
			return false;
		}
		try
		{
			string path = GetPath();
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Paths.ConfigPath);
			File.Copy(backupFilePath, path, overwrite: true);
			s_sidecarConsumedPickupIds.Clear();
			MarkUnloaded();
			SaveData saveData = LoadSaveDataFromDiskNormalized();
			LoadForCurrentSession();
			s_restorePlayerTechStacks.Clear();
			MergePlayerTechStacks(saveData.byPlayerTechStacks, s_restorePlayerTechStacks);
			if ((Object)(object)Inventory.main != (Object)null)
			{
				ForceApplyAllLiveContainersFromSidecar();
				StackIconRefresher.ForceRefresh();
				userMessage = "Stack counts restored from backup.";
			}
			else
			{
				userMessage = "Stack backup restored to disk. Load a save for in-world counts to apply.";
			}
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogInfo((object)("StackUidPersistence restored from " + backupFilePath));
			}
			return true;
		}
		catch (Exception ex)
		{
			userMessage = "Restore failed: " + ex.Message;
			ManualLogSource log2 = Plugin.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("StackUidPersistence restore failed: " + ex.Message));
			}
			return false;
		}
	}

	public static bool TryRestoreFromBackup(out string userMessage)
	{
		return TryRestoreFromBackup(StackBackupRing.GetNewestBackupPath(), out userMessage);
	}

	private static void ForceApplyAllLiveContainersFromSidecar()
	{
		if ((Object)(object)Inventory.main == (Object)null)
		{
			return;
		}
		ApplyPlayerInventorySidecarAfterDeserialize();
		ItemsContainer container = Inventory.main.container;
		if (container != null)
		{
			ForceApplyPlayerPocketExact(container);
		}
		int usedStorageCount = Inventory.main.GetUsedStorageCount();
		for (int i = 0; i < usedStorageCount; i++)
		{
			IItemsContainer usedStorage = Inventory.main.GetUsedStorage(i);
			ItemsContainer val = (ItemsContainer)(object)((usedStorage is ItemsContainer) ? usedStorage : null);
			if (val != null)
			{
				ForceApplyContainerSidecarExact(val);
			}
		}
		HashSet<ItemsContainer> hashSet = new HashSet<ItemsContainer>();
		if (container != null)
		{
			hashSet.Add(container);
		}
		CaptureWorldStorageContainersForForceApply(hashSet);
	}

	private static void CaptureWorldStorageContainersForForceApply(HashSet<ItemsContainer> capturedContainers)
	{
		try
		{
			CaptureByType("StorageContainer");
			CaptureByType("SeamothStorageContainer");
			CaptureByType("SeaMothStorageContainer");
			CaptureByType("ExosuitStorageContainer");
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("ForceApply world storage scan failed: " + ex.Message));
			}
		}
		void CaptureByType(string typeName)
		{
			Type type = AccessTools.TypeByName(typeName);
			if (!(type == null))
			{
				List<Object> list = CollectOwnersOfTypeInLoadedScenes(type);
				for (int i = 0; i < list.Count; i++)
				{
					Object val = list[i];
					if (!(val == (Object)null))
					{
						ItemsContainer value = Traverse.Create((object)val).Field<ItemsContainer>("container").Value;
						if (value != null && capturedContainers.Add(value))
						{
							ForceApplyContainerSidecarExact(value);
						}
					}
				}
			}
		}
	}

	private static void ForceApplyContainerSidecarExact(ItemsContainer container)
	{
		if (container == null || ((Object)(object)Inventory.main != (Object)null && container == Inventory.main.container))
		{
			return;
		}
		LoadForCurrentSession();
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			ForceApplyOneExact(item, container, playerPocket: false);
		}
	}

	private static void ForceApplyPlayerPocketExact(ItemsContainer pocket)
	{
		if (pocket == null)
		{
			return;
		}
		LoadForCurrentSession();
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)pocket)
		{
			ForceApplyOneExact(item, pocket, playerPocket: true);
		}
		ApplyPlayerPocketTechFallback(pocket);
		ApplyRestorePlayerTechStacksFallback(pocket);
		TryConsolidatePlayerInventoryStacks(pocket);
	}

	private static void ForceApplyOneExact(InventoryItem ii, ItemsContainer container, bool playerPocket)
	{
		if ((Object)(object)((ii != null) ? ii.item : null) == (Object)null || !StackRules.CanStack(ii.item) || LifepodStorageScope.IsLifepodOrTimeCapsuleStorage(container) || WaterParkStorageScope.IsCreatureHabitatContainer(container))
		{
			return;
		}
		Pickupable item = ii.item;
		UniqueIdentifier component = ((Component)item).GetComponent<UniqueIdentifier>();
		string text = BuildSlotKey(container, ii);
		string text2 = (((Object)(object)component != (Object)null) ? component.Id : null);
		int value2;
		if (!string.IsNullOrEmpty(text2) && s_loadedUid.TryGetValue(text2, out var value) && value > 1)
		{
			if (playerPocket)
			{
				ApplyExactCountForPlayer(item, value);
			}
			else
			{
				ApplyExactCountFromSidecar(item, value);
			}
		}
		else if (!string.IsNullOrEmpty(text) && s_loadedSlot.TryGetValue(text, out value2) && value2 > 1)
		{
			if (playerPocket)
			{
				ApplyExactCountForPlayer(item, value2);
			}
			else
			{
				ApplyExactCountFromSidecar(item, value2);
			}
		}
	}

	private static void ApplyExactCountFromSidecar(Pickupable p, int jsonCount)
	{
		if (!((Object)(object)p == (Object)null) && jsonCount > 1 && MRStack.CountOf(p) != jsonCount)
		{
			MRStack.SetAmount(p, jsonCount);
		}
	}

	private static void CaptureWorldStorageContainers(SaveData data, HashSet<ItemsContainer> capturedContainers)
	{
		try
		{
			CaptureContainersByOwnerTypeName("StorageContainer", data, capturedContainers);
			CaptureContainersByOwnerTypeName("SeamothStorageContainer", data, capturedContainers);
			CaptureContainersByOwnerTypeName("SeaMothStorageContainer", data, capturedContainers);
			CaptureContainersByOwnerTypeName("ExosuitStorageContainer", data, capturedContainers);
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogWarning((object)("StackUidPersistence world storage scan failed: " + ex.Message));
			}
		}
	}

	private static List<Object> CollectOwnersOfTypeInLoadedScenes(Type ownerType)
	{
		List<Object> owners = new List<Object>();
		if (ownerType == null)
		{
			return owners;
		}
		HashSet<int> seen = new HashSet<int>();
		Object[] array = Object.FindObjectsOfType(ownerType);
		for (int i = 0; i < array.Length; i++)
		{
			Consider(array[i]);
		}
		array = Resources.FindObjectsOfTypeAll(ownerType);
		for (int i = 0; i < array.Length; i++)
		{
			Consider(array[i]);
		}
		return owners;
		void Consider(Object o)
		{
			if (!(o == (Object)null) && seen.Add(o.GetInstanceID()))
			{
				Component val = (Component)(object)((o is Component) ? o : null);
				if (val != null)
				{
					GameObject gameObject = val.gameObject;
					Scene scene = gameObject.scene;
					if (!scene.IsValid())
					{
						return;
					}
					scene = gameObject.scene;
					if (!scene.isLoaded)
					{
						return;
					}
				}
				owners.Add(o);
			}
		}
	}

	private static void CaptureContainersByOwnerTypeName(string ownerTypeName, SaveData data, HashSet<ItemsContainer> capturedContainers)
	{
		if (string.IsNullOrEmpty(ownerTypeName))
		{
			return;
		}
		Type type = AccessTools.TypeByName(ownerTypeName);
		if (type == null)
		{
			return;
		}
		List<Object> list = CollectOwnersOfTypeInLoadedScenes(type);
		for (int i = 0; i < list.Count; i++)
		{
			Object val = list[i];
			if (val == (Object)null)
			{
				continue;
			}
			ItemsContainer value = Traverse.Create((object)val).Field<ItemsContainer>("container").Value;
			if (value == null)
			{
				value = Traverse.Create((object)val).Property<ItemsContainer>("container", (object[])null).Value;
			}
			if (value == null || !capturedContainers.Add(value))
			{
				continue;
			}
			foreach (InventoryItem item in (IEnumerable<InventoryItem>)value)
			{
				CaptureOne(data, value, item);
			}
		}
	}

	private static void CaptureOne(SaveData data, ItemsContainer container, InventoryItem ii)
	{
		if (WaterParkStorageScope.IsCreatureHabitatContainer(container))
		{
			return;
		}
		Pickupable val = ((ii != null) ? ii.item : null);
		if ((Object)(object)val == (Object)null || !StackRules.CanStack(val))
		{
			return;
		}
		int num = MRStack.CountOf(val);
		if (num > 1)
		{
			string text = BuildSlotKey(container, ii);
			if (!string.IsNullOrEmpty(text))
			{
				data.bySlotKey[text] = num;
			}
			UniqueIdentifier component = ((Component)val).GetComponent<UniqueIdentifier>();
			if ((Object)(object)component != (Object)null && !string.IsNullOrEmpty(component.Id))
			{
				data.byPickupUid[component.Id] = num;
			}
		}
	}

	private static void CapturePlayerTechStacks(SaveData data, ItemsContainer pocket)
	{
		if (data == null || pocket == null)
		{
			return;
		}
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)pocket)
		{
			Pickupable val = ((item != null) ? item.item : null);
			if ((Object)(object)val == (Object)null || !StackRules.CanStack(val))
			{
				continue;
			}
			int num = MRStack.CountOf(val);
			if (num > 1)
			{
				string key = ((int)val.GetTechType()).ToString();
				if (!data.byPlayerTechStacks.TryGetValue(key, out var value))
				{
					value = new List<int>();
					data.byPlayerTechStacks[key] = value;
				}
				value.Add(num);
			}
		}
		foreach (KeyValuePair<string, List<int>> byPlayerTechStack in data.byPlayerTechStacks)
		{
			if (byPlayerTechStack.Value != null && byPlayerTechStack.Value.Count > 1)
			{
				byPlayerTechStack.Value.Sort((int a, int b) => b.CompareTo(a));
			}
		}
	}

	private static string BuildSlotKey(ItemsContainer container, InventoryItem ii)
	{
		if (container == null || (Object)(object)((ii != null) ? ii.item : null) == (Object)null)
		{
			return null;
		}
		string containerStablePrefix = GetContainerStablePrefix(container);
		TechType techType = ii.item.GetTechType();
		return containerStablePrefix + "|" + (int)techType + "|" + ii.x + "|" + ii.y;
	}

	private static string GetContainerStablePrefix(ItemsContainer container)
	{
		try
		{
			if ((Object)(object)Inventory.main != (Object)null && container == Inventory.main.container)
			{
				return "player";
			}
			Transform tr = container.tr;
			if ((Object)(object)tr == (Object)null)
			{
				return "notr";
			}
			Transform val = tr;
			int num = 0;
			while ((Object)(object)val != (Object)null && num < 28)
			{
				UniqueIdentifier component = ((Component)val).GetComponent<UniqueIdentifier>();
				if ((Object)(object)component != (Object)null && !string.IsNullOrEmpty(component.Id))
				{
					return "u:" + component.Id;
				}
				val = val.parent;
				num++;
			}
			return "path:" + BuildTransformPath(tr);
		}
		catch
		{
			return "err";
		}
	}

	private static string BuildTransformPath(Transform tr)
	{
		string text = ((Object)tr).name;
		Transform parent = tr.parent;
		int num = 0;
		while ((Object)(object)parent != (Object)null && num < 32)
		{
			text = ((Object)parent).name + "/" + text;
			parent = parent.parent;
			num++;
		}
		return text;
	}

	internal static void TryApplyContainerSidecar(ItemsContainer container)
	{
		if (container == null)
		{
			return;
		}
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			TryApply(item, container);
		}
	}

	internal static void ScheduleSeamothDeserializeSidecarRetries(ItemsContainer container)
	{
		if (container != null && !((Object)(object)Plugin.Instance == (Object)null) && s_seamothSidecarRetryScheduled.Add(container))
		{
			((MonoBehaviour)Plugin.Instance).StartCoroutine(CoRetrySeamothStorageSidecar(container));
		}
	}

	private static IEnumerator CoRetrySeamothStorageSidecar(ItemsContainer container)
	{
		for (int attempt = 0; attempt < 6; attempt++)
		{
			if (attempt > 0)
			{
				yield return null;
			}
			if (container == null)
			{
				break;
			}
			TryApplyContainerSidecar(container);
		}
	}

	public static void TryApply(InventoryItem ii, ItemsContainer container)
	{
		if ((Object)(object)((ii != null) ? ii.item : null) == (Object)null || !StackRules.CanStack(ii.item) || LifepodStorageScope.IsLifepodOrTimeCapsuleStorage(container) || WaterParkStorageScope.IsCreatureHabitatContainer(container) || ((Object)(object)Inventory.main != (Object)null && container == Inventory.main.container))
		{
			return;
		}
		Pickupable item = ii.item;
		int instanceID = ((Object)((Component)item).gameObject).GetInstanceID();
		if (s_sidecarConsumedPickupIds.Contains(instanceID))
		{
			return;
		}
		LoadForCurrentSession();
		UniqueIdentifier component = ((Component)item).GetComponent<UniqueIdentifier>();
		string text = BuildSlotKey(container, ii);
		int value2;
		if ((Object)(object)component != (Object)null && !string.IsNullOrEmpty(component.Id) && s_loadedUid.TryGetValue(component.Id, out var value) && value > 1)
		{
			ApplyCountFromSidecar(item, value);
			s_loadedUid.Remove(component.Id);
			if (!string.IsNullOrEmpty(text))
			{
				s_loadedSlot.Remove(text);
			}
			s_sidecarConsumedPickupIds.Add(instanceID);
		}
		else if (!string.IsNullOrEmpty(text) && s_loadedSlot.TryGetValue(text, out value2) && value2 > 1)
		{
			ApplyCountFromSidecar(item, value2);
			s_loadedSlot.Remove(text);
			if ((Object)(object)component != (Object)null && !string.IsNullOrEmpty(component.Id))
			{
				s_loadedUid.Remove(component.Id);
			}
			s_sidecarConsumedPickupIds.Add(instanceID);
		}
	}

	public static void ApplyPlayerInventorySidecarAfterDeserialize()
	{
		LoadForCurrentSession();
		if ((Object)(object)Inventory.main == (Object)null)
		{
			return;
		}
		ItemsContainer container = Inventory.main.container;
		if (container == null)
		{
			return;
		}
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			TryApplyPlayerPocketOne(item, container);
		}
		ApplyPlayerPocketTechFallback(container);
		PruneStalePlayerSlotMappings();
		PruneRemainingPlayerSlotMappings();
		TryConsolidatePlayerInventoryStacks(container);
		if ((Object)(object)Plugin.Instance != (Object)null)
		{
			((MonoBehaviour)Plugin.Instance).StartCoroutine(CoDeferredPlayerConsolidate());
		}
	}

	internal static IEnumerator CoDeferredPlayerConsolidate()
	{
		for (int i = 0; i < 12; i++)
		{
			yield return null;
			Inventory main = Inventory.main;
			ItemsContainer val = ((main != null) ? main.container : null);
			if (val == null)
			{
				yield break;
			}
			TryConsolidatePlayerInventoryStacks(val);
		}
		StackCapEnforcer.EnforceCapOnAllPlayerInventories();
	}

	private static void PruneStalePlayerSlotMappings()
	{
		if (s_loadedSlot.Count == 0)
		{
			return;
		}
		Inventory main = Inventory.main;
		if (((main != null) ? main.container : null) == null)
		{
			return;
		}
		List<string> list = null;
		foreach (string key in s_loadedSlot.Keys)
		{
			if (string.IsNullOrEmpty(key) || !key.StartsWith("player|", StringComparison.Ordinal) || !TryParsePlayerSlotKey(key, out var techId, out var x, out var y))
			{
				continue;
			}
			InventoryItem val = FindInventoryItemAt(Inventory.main.container, x, y);
			if ((Object)(object)((val != null) ? val.item : null) == (Object)null || (int)val.item.GetTechType() != techId)
			{
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(key);
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				s_loadedSlot.Remove(list[i]);
			}
		}
	}

	private static InventoryItem FindInventoryItemAt(ItemsContainer container, int x, int y)
	{
		if (container == null)
		{
			return null;
		}
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
		{
			if (item != null && item.x == x && item.y == y)
			{
				return item;
			}
		}
		return null;
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
		if (array.Length != 4 || array[0] != "player")
		{
			return false;
		}
		if (int.TryParse(array[1], out techId) && int.TryParse(array[2], out x))
		{
			return int.TryParse(array[3], out y);
		}
		return false;
	}

	private static void TryConsolidatePlayerInventoryStacks(ItemsContainer pocket)
	{
		if (pocket == null)
		{
			return;
		}
		List<InventoryItem> list = new List<InventoryItem>();
		foreach (InventoryItem item in (IEnumerable<InventoryItem>)pocket)
		{
			if ((Object)(object)((item != null) ? item.item : null) != (Object)null)
			{
				list.Add(item);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			StackConsolidation.TryMerge(list[i], pocket);
		}
	}

	private static void PruneRemainingPlayerSlotMappings()
	{
		if (s_loadedSlot.Count == 0)
		{
			return;
		}
		List<string> list = null;
		foreach (string key in s_loadedSlot.Keys)
		{
			if (!string.IsNullOrEmpty(key) && key.StartsWith("player|", StringComparison.Ordinal))
			{
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(key);
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				s_loadedSlot.Remove(list[i]);
			}
		}
	}

	private static void ApplyRestorePlayerTechStacksFallback(ItemsContainer pocket)
	{
		if (pocket == null || s_restorePlayerTechStacks.Count == 0)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, List<int>> s_restorePlayerTechStack in s_restorePlayerTechStacks)
		{
			int key = s_restorePlayerTechStack.Key;
			List<int> value = s_restorePlayerTechStack.Value;
			if (value == null || value.Count == 0)
			{
				list.Add(key);
				continue;
			}
			List<Pickupable> list2 = new List<Pickupable>();
			foreach (InventoryItem item in (IEnumerable<InventoryItem>)pocket)
			{
				Pickupable val = ((item != null) ? item.item : null);
				if (!((Object)(object)val == (Object)null) && StackRules.CanStack(val) && (int)val.GetTechType() == key)
				{
					list2.Add(val);
				}
			}
			if (list2.Count == 0)
			{
				continue;
			}
			list2.Sort((Pickupable a, Pickupable b) => MRStack.CountOf(b).CompareTo(MRStack.CountOf(a)));
			int num = Math.Min(list2.Count, value.Count);
			for (int num2 = 0; num2 < num; num2++)
			{
				Pickupable val2 = list2[num2];
				int num3 = value[num2];
				if (num3 > 1)
				{
					if (MRStack.CountOf(val2) != num3)
					{
						MRStack.SetAmount(val2, num3);
					}
					s_sidecarConsumedPickupIds.Add(((Object)((Component)val2).gameObject).GetInstanceID());
				}
			}
			if (num >= value.Count)
			{
				list.Add(key);
			}
			else
			{
				value.RemoveRange(0, num);
			}
		}
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			s_restorePlayerTechStacks.Remove(list[num4]);
		}
	}

	private static void ApplyPlayerPocketTechFallback(ItemsContainer pocket)
	{
		if (pocket == null || s_loadedSlot.Count == 0)
		{
			return;
		}
		Dictionary<int, Queue<int>> dictionary = new Dictionary<int, Queue<int>>();
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> item2 in s_loadedSlot)
		{
			if (string.IsNullOrEmpty(item2.Key) || item2.Value <= 1)
			{
				continue;
			}
			string[] array = item2.Key.Split('|');
			if (array.Length >= 4 && string.Equals(array[0], "player", StringComparison.Ordinal) && int.TryParse(array[1], out var result))
			{
				if (!dictionary.TryGetValue(result, out var value))
				{
					value = new Queue<int>();
					dictionary.Add(result, value);
				}
				value.Enqueue(item2.Value);
				list.Add(item2.Key);
			}
		}
		if (dictionary.Count == 0)
		{
			return;
		}
		foreach (InventoryItem item3 in (IEnumerable<InventoryItem>)pocket)
		{
			if ((Object)(object)((item3 != null) ? item3.item : null) == (Object)null || !StackRules.CanStack(item3.item))
			{
				continue;
			}
			Pickupable item = item3.item;
			int instanceID = ((Object)((Component)item).gameObject).GetInstanceID();
			if (!s_sidecarConsumedPickupIds.Contains(instanceID) && MRStack.CountOf(item) <= 1)
			{
				int key = (int)item.GetTechType();
				if (dictionary.TryGetValue(key, out var value2) && value2.Count != 0)
				{
					int jsonCount = value2.Dequeue();
					ApplyCountFromSidecar(item, jsonCount);
					s_sidecarConsumedPickupIds.Add(instanceID);
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			s_loadedSlot.Remove(list[i]);
		}
	}

	private static void TryApplyPlayerPocketOne(InventoryItem ii, ItemsContainer container)
	{
		if ((Object)(object)((ii != null) ? ii.item : null) == (Object)null || !StackRules.CanStack(ii.item))
		{
			return;
		}
		Pickupable item = ii.item;
		int instanceID = ((Object)((Component)item).gameObject).GetInstanceID();
		if (s_sidecarConsumedPickupIds.Contains(instanceID))
		{
			return;
		}
		UniqueIdentifier component = ((Component)item).GetComponent<UniqueIdentifier>();
		string text = BuildSlotKey(container, ii);
		int value2;
		if ((Object)(object)component != (Object)null && !string.IsNullOrEmpty(component.Id) && s_loadedUid.TryGetValue(component.Id, out var value) && value > 1)
		{
			ApplyExactCountForPlayer(item, value);
			s_loadedUid.Remove(component.Id);
			if (!string.IsNullOrEmpty(text))
			{
				s_loadedSlot.Remove(text);
			}
			s_sidecarConsumedPickupIds.Add(instanceID);
		}
		else if (!string.IsNullOrEmpty(text) && s_loadedSlot.TryGetValue(text, out value2) && value2 > 1)
		{
			ApplyExactCountForPlayer(item, value2);
			s_loadedSlot.Remove(text);
			if ((Object)(object)component != (Object)null && !string.IsNullOrEmpty(component.Id))
			{
				s_loadedUid.Remove(component.Id);
			}
			s_sidecarConsumedPickupIds.Add(instanceID);
		}
	}

	private static void ApplyCountFromSidecar(Pickupable p, int jsonCount)
	{
		if (jsonCount <= 1)
		{
			return;
		}
		int num = MRStack.CountOf(p);
		if (num > 1)
		{
			if (jsonCount < num)
			{
				MRStack.SetAmount(p, jsonCount);
			}
		}
		else
		{
			MRStack.SetAmount(p, jsonCount);
		}
	}

	private static void ApplyExactCountForPlayer(Pickupable p, int jsonCount)
	{
		if (!((Object)(object)p == (Object)null) && jsonCount > 1 && MRStack.CountOf(p) != jsonCount)
		{
			MRStack.SetAmount(p, jsonCount);
		}
	}

	private static string GetPath()
	{
		return InventoryStackingPaths.StackSidecarPath;
	}
}
