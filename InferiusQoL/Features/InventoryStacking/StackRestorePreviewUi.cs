#nullable disable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace InferiusQoL.Features.InventoryStacking;

internal sealed class StackRestorePreviewUi : MonoBehaviour
{
	private const float CellSize = 56f;

	private const float CellGap = 4f;

	private const float HeaderHeight = 76f;

	private const float BackupNavBarHeight = 54f;

	private const float ColumnHeaderHeight = 28f;

	private const float FooterHeight = 56f;

	private const float ColumnPadding = 16f;

	private static StackRestorePreviewUi s_instance;

	private GameObject _root;

	private RectTransform _dialogRt;

	private RectTransform _cancelBtnRt;

	private RectTransform _restoreBtnRt;

	private RectTransform _olderBtnRt;

	private RectTransform _newerBtnRt;

	private bool _olderBtnEnabled;

	private bool _newerBtnEnabled;

	private static TMP_FontAsset s_font;

	private StackRestorePlayerPage _currentPage;

	private IReadOnlyList<StackBackupRing.BackupInfo> _backups = Array.Empty<StackBackupRing.BackupInfo>();

	private int _selectedBackupIndex;

	public static void TryShow()
	{
		if (!StackUidPersistence.BackupExists())
		{
			StackRestoreOptionsFeedback.Show("No stack backup yet. Play and save to create one.");
			return;
		}
		if (!StackRestorePreviewData.TryLoadPlayerFirstPage(StackUidPersistence.GetSidecarPath(), out var page))
		{
			StackRestoreOptionsFeedback.Show("Could not read current stack data file.");
			return;
		}
		IReadOnlyList<StackBackupRing.BackupInfo> backupsOldestFirst = StackBackupRing.GetBackupsOldestFirst();
		if (backupsOldestFirst.Count == 0)
		{
			StackRestoreOptionsFeedback.Show("No stack backup yet. Play and save to create one.");
			return;
		}
		EnsureInstance();
		s_instance.Present(page, backupsOldestFirst);
	}

	private static void EnsureInstance()
	{
		if (!((Object)(object)s_instance != (Object)null))
		{
			GameObject val = new GameObject("StackRestorePreview");
			Object.DontDestroyOnLoad((Object)val);
			s_instance = val.AddComponent<StackRestorePreviewUi>();
		}
	}

	private void Present(StackRestorePlayerPage current, IReadOnlyList<StackBackupRing.BackupInfo> backups)
	{
		_currentPage = current;
		_backups = backups;
		_selectedBackupIndex = Math.Max(0, backups.Count - 1);
		RebuildUi();
	}

	private void RebuildUi()
	{
		DestroyRoot();
		if (_backups != null && _backups.Count != 0)
		{
			StackBackupRing.BackupInfo backupInfo = _backups[_selectedBackupIndex];
			if (!StackRestorePreviewData.TryLoadPlayerFirstPage(backupInfo.FilePath, out var page))
			{
				StackRestoreOptionsFeedback.Show("Could not read the selected stack backup file.");
			}
			else
			{
				BuildUi(_currentPage, page, backupInfo);
			}
		}
	}

	private void Update()
	{
		if ((Object)(object)_root == (Object)null || !_root.activeSelf)
		{
			return;
		}
		if (GameInput.GetButtonDown(GameInput.Button.UICancel))
		{
			Close();
			GameInput.ClearInput(0);
		}
		else if (Input.GetKeyDown((KeyCode)276) || Input.GetKeyDown((KeyCode)97))
		{
			SelectOlderBackup();
		}
		else if (Input.GetKeyDown((KeyCode)275) || Input.GetKeyDown((KeyCode)100))
		{
			SelectNewerBackup();
		}
		else if (Input.GetMouseButtonDown(0))
		{
			Vector2 screenPoint = Input.mousePosition;
			if (ContainsScreenPoint(_restoreBtnRt, screenPoint))
			{
				ExecuteRestore();
			}
			else if (ContainsScreenPoint(_cancelBtnRt, screenPoint))
			{
				Close();
			}
			else if (_olderBtnEnabled && ContainsScreenPoint(_olderBtnRt, screenPoint))
			{
				SelectOlderBackup();
			}
			else if (_newerBtnEnabled && ContainsScreenPoint(_newerBtnRt, screenPoint))
			{
				SelectNewerBackup();
			}
			else if ((Object)(object)_dialogRt != (Object)null && !ContainsScreenPoint(_dialogRt, screenPoint))
			{
				Close();
			}
		}
	}

	private void SelectOlderBackup()
	{
		if (_selectedBackupIndex > 0)
		{
			_selectedBackupIndex--;
			RebuildUi();
		}
	}

	private void SelectNewerBackup()
	{
		if (_selectedBackupIndex < _backups.Count - 1)
		{
			_selectedBackupIndex++;
			RebuildUi();
		}
	}

	private static bool ContainsScreenPoint(RectTransform rt, Vector2 screenPoint)
	{
		if ((Object)(object)rt != (Object)null && ((Component)rt).gameObject.activeInHierarchy)
		{
			return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, (Camera)null);
		}
		return false;
	}

	private void Close()
	{
		DestroyRoot();
	}

	private void DestroyRoot()
	{
		_dialogRt = null;
		_cancelBtnRt = null;
		_restoreBtnRt = null;
		_olderBtnRt = null;
		_newerBtnRt = null;
		_olderBtnEnabled = false;
		_newerBtnEnabled = false;
		if ((Object)(object)_root != (Object)null)
		{
			Object.Destroy((Object)(object)_root);
			_root = null;
		}
	}

	private void OnDestroy()
	{
		if ((Object)(object)s_instance == (Object)(object)this)
		{
			s_instance = null;
		}
	}

	private void BuildUi(StackRestorePlayerPage current, StackRestorePlayerPage backup, StackBackupRing.BackupInfo backupInfo)
	{
		EnsureFont();
		_root = new GameObject("Root");
		_root.transform.SetParent(((Component)this).transform, false);
		Canvas obj = _root.AddComponent<Canvas>();
		obj.renderMode = (RenderMode)0;
		obj.sortingOrder = 10000;
		_root.AddComponent<GraphicRaycaster>();
		CanvasScaler obj2 = _root.AddComponent<CanvasScaler>();
		obj2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		obj2.referenceResolution = new Vector2(1920f, 1080f);
		CreatePanel(_root.transform, "Backdrop", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.72f));
		float num = 356f;
		float num2 = 236f;
		float num3 = num + 32f;
		float num4 = 28f + num2 + 16f;
		float num5 = num3 * 2f + 48f;
		float num6 = 130f + num4 + 56f + 16f;
		_dialogRt = CreatePanel(_root.transform, "Dialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.08f, 0.1f, 0.12f, 0.98f));
		_dialogRt.sizeDelta = new Vector2(num5, num6);
		_dialogRt.anchoredPosition = Vector2.zero;
		((Component)_dialogRt).gameObject.AddComponent<RectMask2D>();
		AddTitle(_dialogRt, "Restore stack backup?");
		AddSubtitle(_dialogRt, "Inventory page 1 (top 4 rows) — current file (left) vs selected backup (right). Use ◀ ▶ or arrow keys.");
		float num7 = 76f;
		float num8 = 138f;
		AddBackupNavBar(_dialogRt, backupInfo);
		RectTransform parent = CreateStretchRegion(_dialogRt, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 10f), new Vector2(-20f, 66f));
		_cancelBtnRt = CreateActionButton(parent, "Cancel", new Vector2(0.28f, 0.5f), new Color(0.25f, 0.28f, 0.32f));
		_restoreBtnRt = CreateActionButton(parent, "Restore backup", new Vector2(0.72f, 0.5f), new Color(0.12f, 0.38f, 0.22f));
		RectTransform parent2 = CreateStretchRegion(_dialogRt, "Body", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, num7), new Vector2(-12f, 0f - num8));
		RectTransform obj3 = CreateStretchRegion(parent2, "CurrentColumn", new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(4f, 4f), new Vector2(-6f, -4f));
		AddColumnHeader(obj3, "Current");
		BuildGrid(obj3, current, backup, num, num2, 36f, isBackupColumn: false);
		RectTransform obj4 = CreateStretchRegion(parent2, "BackupColumn", new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(6f, 4f), new Vector2(-4f, -4f));
		AddBackupColumnHeader(obj4, backupInfo);
		BuildGrid(obj4, backup, current, num, num2, 36f, isBackupColumn: true);
	}

	private void AddBackupNavBar(RectTransform parent, StackBackupRing.BackupInfo backupInfo)
	{
		int num = backupInfo.InventoryItemCount;
		if (num <= 0)
		{
			num = StackRestorePreviewData.CountPlayerInventoryUnits(backupInfo.FilePath);
		}
		RectTransform parent2 = CreateStretchRegion(parent, "BackupNavBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -130f), new Vector2(-16f, -76f));
		_olderBtnEnabled = _selectedBackupIndex > 0;
		_newerBtnEnabled = _selectedBackupIndex < _backups.Count - 1;
		_olderBtnRt = CreateNavButton(parent2, "◀ Older", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), _olderBtnEnabled);
		_newerBtnRt = CreateNavButton(parent2, "Newer ▶", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-110f, 0f), _newerBtnEnabled);
		string text = ((_backups.Count <= 1) ? ("Backup 1 of 1 — " + backupInfo.GetSaveLabel()) : $"Backup {_selectedBackupIndex + 1} of {_backups.Count} — {backupInfo.GetSaveLabel()}");
		string text2 = backupInfo.FormatSaveSlotTimeLocal();
		string text3 = backupInfo.FormatBackupTimeLocal();
		string text4 = ((text2 != null) ? $"Save game {text2} · Stack snapshot {text3} · {num} items" : $"Stack snapshot {text3} · {num} items");
		TextMeshProUGUI obj = CreateTmp((Transform)(object)parent2, "NavLabel1", text, 15f, (FontStyles)1);
		RectTransform rectTransform = ((TMP_Text)obj).rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchoredPosition = new Vector2(0f, 8f);
		rectTransform.sizeDelta = new Vector2(760f, 22f);
		((Graphic)obj).color = new Color(0.88f, 0.92f, 0.96f);
		((TMP_Text)obj).enableWordWrapping = false;
		((TMP_Text)obj).overflowMode = (TextOverflowModes)1;
		TextMeshProUGUI obj2 = CreateTmp((Transform)(object)parent2, "NavLabel2", text4, 13f, (FontStyles)0);
		RectTransform rectTransform2 = ((TMP_Text)obj2).rectTransform;
		rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform2.pivot = new Vector2(0.5f, 0.5f);
		rectTransform2.anchoredPosition = new Vector2(0f, -10f);
		rectTransform2.sizeDelta = new Vector2(760f, 20f);
		((Graphic)obj2).color = new Color(0.72f, 0.78f, 0.84f);
		((TMP_Text)obj2).enableWordWrapping = false;
		((TMP_Text)obj2).overflowMode = (TextOverflowModes)1;
	}

	private static void AddBackupColumnHeader(RectTransform parent, StackBackupRing.BackupInfo backupInfo)
	{
		int num = backupInfo.InventoryItemCount;
		if (num <= 0)
		{
			num = StackRestorePreviewData.CountPlayerInventoryUnits(backupInfo.FilePath);
		}
		string saveLabel = backupInfo.GetSaveLabel();
		string text = backupInfo.FormatSaveSlotTimeLocal();
		AddColumnHeader(parent, (text != null) ? (saveLabel + " — save " + text) : saveLabel);
		AddColumnSubheader(parent, $"Stack snapshot {backupInfo.FormatBackupTimeLocal()} · {num} items");
	}

	private static void AddTitle(RectTransform parent, string text)
	{
		RectTransform rectTransform = ((TMP_Text)CreateTmp((Transform)(object)parent, "Title", text, 26f, (FontStyles)1)).rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 1f);
		rectTransform.anchorMax = new Vector2(0.5f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 1f);
		rectTransform.anchoredPosition = new Vector2(0f, -10f);
		rectTransform.sizeDelta = new Vector2(900f, 34f);
	}

	private static void AddSubtitle(RectTransform parent, string text)
	{
		TextMeshProUGUI obj = CreateTmp((Transform)(object)parent, "Subtitle", text, 15f, (FontStyles)0);
		RectTransform rectTransform = ((TMP_Text)obj).rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 1f);
		rectTransform.anchorMax = new Vector2(0.5f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 1f);
		rectTransform.anchoredPosition = new Vector2(0f, -42f);
		rectTransform.sizeDelta = new Vector2(900f, 26f);
		((Graphic)obj).color = new Color(0.75f, 0.8f, 0.85f);
	}

	private static void AddColumnHeader(RectTransform parent, string text)
	{
		RectTransform rectTransform = ((TMP_Text)CreateTmp((Transform)(object)parent, "Header", text, 18f, (FontStyles)1)).rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 1f);
		rectTransform.anchorMax = new Vector2(0.5f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 1f);
		rectTransform.anchoredPosition = new Vector2(0f, -4f);
		rectTransform.sizeDelta = new Vector2(320f, 24f);
	}

	private static void AddColumnSubheader(RectTransform parent, string text)
	{
		TextMeshProUGUI obj = CreateTmp((Transform)(object)parent, "Subheader", text, 14f, (FontStyles)0);
		RectTransform rectTransform = ((TMP_Text)obj).rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 1f);
		rectTransform.anchorMax = new Vector2(0.5f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 1f);
		rectTransform.anchoredPosition = new Vector2(0f, -26f);
		rectTransform.sizeDelta = new Vector2(320f, 20f);
		((Graphic)obj).color = new Color(0.72f, 0.78f, 0.84f);
	}

	private static RectTransform CreateNavButton(RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, bool enabled)
	{
		GameObject val = new GameObject(label.Replace(" ", ""));
		val.transform.SetParent((Transform)(object)parent, false);
		RectTransform val2 = val.AddComponent<RectTransform>();
		val2.anchorMin = anchorMin;
		val2.anchorMax = anchorMax;
		val2.pivot = new Vector2(0.5f, 0.5f);
		val2.anchoredPosition = anchoredPosition;
		val2.sizeDelta = new Vector2(200f, 36f);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = (enabled ? new Color(0.22f, 0.28f, 0.34f, 0.98f) : new Color(0.14f, 0.16f, 0.18f, 0.65f));
		((Graphic)obj).raycastTarget = false;
		TextMeshProUGUI obj2 = CreateTmp(val.transform, "Label", label, 17f, (FontStyles)1);
		RectTransform rectTransform = ((TMP_Text)obj2).rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		((Graphic)obj2).color = (Color)(enabled ? Color.white : new Color(0.5f, 0.54f, 0.58f));
		return val2;
	}

	private static void BuildGrid(RectTransform column, StackRestorePlayerPage page, StackRestorePlayerPage other, float gridW, float gridH, float topOffset, bool isBackupColumn)
	{
		GameObject val = new GameObject("Grid");
		val.transform.SetParent((Transform)(object)column, false);
		RectTransform val2 = val.AddComponent<RectTransform>();
		val2.anchorMin = new Vector2(0.5f, 1f);
		val2.anchorMax = new Vector2(0.5f, 1f);
		val2.pivot = new Vector2(0.5f, 1f);
		val2.anchoredPosition = new Vector2(0f, 0f - topOffset);
		val2.sizeDelta = new Vector2(gridW, gridH);
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				StackRestoreSlot slot = page.Get(j, i);
				StackRestoreSlot other2 = other.Get(j, i);
				bool differs = !slot.SameStackAs(other2);
				CreateCell(val2, j, i, slot, differs, isBackupColumn);
			}
		}
	}

	private static void CreateCell(RectTransform grid, int x, int y, StackRestoreSlot slot, bool differs, bool isBackupColumn)
	{
		GameObject val = new GameObject($"Cell_{x}_{y}");
		val.transform.SetParent((Transform)(object)grid, false);
		RectTransform obj = val.AddComponent<RectTransform>();
		float num = (float)x * 60f;
		float num2 = (float)(3 - y) * 60f;
		obj.anchorMin = new Vector2(0f, 1f);
		obj.anchorMax = new Vector2(0f, 1f);
		obj.pivot = new Vector2(0f, 1f);
		obj.anchoredPosition = new Vector2(num, 0f - num2);
		obj.sizeDelta = new Vector2(56f, 56f);
		Color color = ((!differs) ? new Color(0.14f, 0.16f, 0.18f, 0.9f) : (isBackupColumn ? new Color(0.15f, 0.28f, 0.18f, 0.95f) : new Color(0.32f, 0.18f, 0.12f, 0.95f)));
		Image obj2 = val.AddComponent<Image>();
		((Graphic)obj2).color = color;
		((Graphic)obj2).raycastTarget = false;
		if (slot.IsEmpty)
		{
			return;
		}
		try
		{
			Sprite val2 = SpriteManager.Get(slot.Tech);
			if ((Object)(object)val2 != (Object)null)
			{
				GameObject val3 = new GameObject("Icon");
				val3.transform.SetParent(val.transform, false);
				RectTransform obj3 = val3.AddComponent<RectTransform>();
				obj3.anchorMin = Vector2.zero;
				obj3.anchorMax = Vector2.one;
				obj3.offsetMin = new Vector2(4f, 4f);
				obj3.offsetMax = new Vector2(-4f, -14f);
				Image obj4 = val3.AddComponent<Image>();
				obj4.sprite = val2;
				obj4.preserveAspect = true;
				((Graphic)obj4).raycastTarget = false;
			}
		}
		catch
		{
		}
		string itemLabel = GetItemLabel(slot.Tech);
		TextMeshProUGUI obj6 = CreateTmp(val.transform, "Name", itemLabel, 9f, (FontStyles)0);
		RectTransform rectTransform = ((TMP_Text)obj6).rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(1f, 0f);
		rectTransform.pivot = new Vector2(0.5f, 0f);
		rectTransform.anchoredPosition = new Vector2(0f, 2f);
		rectTransform.sizeDelta = new Vector2(0f, 12f);
		((TMP_Text)obj6).alignment = (TextAlignmentOptions)1026;
		((TMP_Text)obj6).enableWordWrapping = false;
		((TMP_Text)obj6).overflowMode = (TextOverflowModes)1;
		if (slot.Count > 1)
		{
			TextMeshProUGUI obj7 = CreateTmp(val.transform, "Count", slot.Count.ToString(), 14f, (FontStyles)1);
			RectTransform rectTransform2 = ((TMP_Text)obj7).rectTransform;
			rectTransform2.anchorMin = new Vector2(1f, 1f);
			rectTransform2.anchorMax = new Vector2(1f, 1f);
			rectTransform2.pivot = new Vector2(1f, 1f);
			rectTransform2.anchoredPosition = new Vector2(-2f, -1f);
			rectTransform2.sizeDelta = new Vector2(44f, 18f);
			((TMP_Text)obj7).alignment = (TextAlignmentOptions)260;
			((TMP_Text)obj7).enableWordWrapping = false;
			((TMP_Text)obj7).overflowMode = (TextOverflowModes)0;
			((TMP_Text)obj7).margin = new Vector4(0f, 0f, 2f, 0f);
		}
	}

	private static string GetItemLabel(TechType tech)
	{
		try
		{
			if ((Object)(object)Language.main != (Object)null)
			{
				string text = Language.main.Get(TechTypeExtensions.AsString(tech, false));
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return tech.ToString();
	}

	private void ExecuteRestore()
	{
		if (_backups != null && _backups.Count != 0 && _selectedBackupIndex >= 0 && _selectedBackupIndex < _backups.Count)
		{
			string filePath = _backups[_selectedBackupIndex].FilePath;
			Close();
			StackUidPersistence.TryRestoreFromBackup(filePath, out var userMessage);
			StackRestoreOptionsFeedback.Show(userMessage);
		}
	}

	private static RectTransform CreateActionButton(RectTransform parent, string label, Vector2 anchorX, Color color)
	{
		GameObject val = new GameObject(label);
		val.transform.SetParent((Transform)(object)parent, false);
		RectTransform val2 = val.AddComponent<RectTransform>();
		val2.anchorMin = new Vector2(anchorX.x, 0.5f);
		val2.anchorMax = new Vector2(anchorX.x, 0.5f);
		val2.pivot = new Vector2(0.5f, 0.5f);
		val2.anchoredPosition = Vector2.zero;
		val2.sizeDelta = new Vector2(220f, 40f);
		Image obj = val.AddComponent<Image>();
		((Graphic)obj).color = color;
		((Graphic)obj).raycastTarget = false;
		TextMeshProUGUI obj2 = CreateTmp(val.transform, "Label", label, 18f, (FontStyles)1);
		RectTransform rectTransform = ((TMP_Text)obj2).rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		((TMP_Text)obj2).alignment = (TextAlignmentOptions)514;
		return val2;
	}

	private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
	{
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		RectTransform obj = val.AddComponent<RectTransform>();
		obj.anchorMin = anchorMin;
		obj.anchorMax = anchorMax;
		obj.offsetMin = Vector2.zero;
		obj.offsetMax = Vector2.zero;
		Image obj2 = val.AddComponent<Image>();
		((Graphic)obj2).color = color;
		((Graphic)obj2).raycastTarget = false;
		return obj;
	}

	private static RectTransform CreateStretchRegion(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
	{
		GameObject val = new GameObject(name);
		val.transform.SetParent((Transform)(object)parent, false);
		RectTransform obj = val.AddComponent<RectTransform>();
		obj.anchorMin = anchorMin;
		obj.anchorMax = anchorMax;
		obj.offsetMin = offsetMin;
		obj.offsetMax = offsetMax;
		Image obj2 = val.AddComponent<Image>();
		((Graphic)obj2).color = new Color(0.12f, 0.14f, 0.16f, 0.55f);
		((Graphic)obj2).raycastTarget = false;
		return obj;
	}

	private static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize, FontStyles style)
	{
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		TextMeshProUGUI val2 = val.AddComponent<TextMeshProUGUI>();
		if ((Object)(object)s_font != (Object)null)
		{
			((TMP_Text)val2).font = s_font;
		}
		((TMP_Text)val2).text = text;
		((TMP_Text)val2).fontSize = fontSize;
		((TMP_Text)val2).fontStyle = style;
		((Graphic)val2).color = Color.white;
		((Graphic)val2).raycastTarget = false;
		((TMP_Text)val2).alignment = (TextAlignmentOptions)514;
		return val2;
	}

	private static void EnsureFont()
	{
		if ((Object)(object)s_font != (Object)null)
		{
			return;
		}
		TextMeshProUGUI[] array = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
		foreach (TextMeshProUGUI val in array)
		{
			if ((Object)(object)val != (Object)null && (Object)(object)((TMP_Text)val).font != (Object)null)
			{
				s_font = ((TMP_Text)val).font;
				break;
			}
		}
	}
}
