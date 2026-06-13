namespace InferiusQoL.Features.MobileResourceScanner;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class MobileResourceScannerFeature
{
    private const int MenuWidth = 820;
    private const int MenuHeight = 720;
    private const int TileColumns = 4;
    private const int TileWidth = 178;
    private const int TileHeight = 98;
    private const int TileSpacing = 10;

    private static readonly Color PanelColor = new Color(0.02f, 0.07f, 0.09f, 0.92f);
    private static readonly Color HeaderColor = new Color(0.04f, 0.22f, 0.27f, 0.88f);
    private static readonly Color ScrollColor = new Color(0.0f, 0.03f, 0.04f, 0.38f);
    private static readonly Color DividerColor = new Color(0.45f, 0.82f, 0.88f, 0.55f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.88f, 0.9f, 0.86f);
    private static readonly Color BrightTextColor = new Color(0.92f, 1f, 1f, 0.98f);

    internal static GameObject? MenuGameObject { get; private set; }

    private static TechType _currentTechType = TechType.None;
    private static string _currentTechName = "None";
    private static int _activeIntervalSeconds = -1;
    private static float _nextPowerWarningTime;
    private static bool _closingMenu;

    private static InferiusConfig Config => InferiusConfig.Instance;

    internal static bool Enabled => Config.MobileResourceScannerEnabled;

    internal static void Init()
    {
        RestoreCurrentResource();
        QoLLog.Info(Category.MobileScanner, $"Mobile Resource Scanner initialized; selected resource: {_currentTechName}");
    }

    internal static void Update()
    {
        if (!Enabled
            || Player.main?.GetPDA()?.isInUse != false
            || FPSInputModule.current?.lastGroup != null
            || !IsMenuHotkeyDown())
        {
            return;
        }

        ShowMenu();
    }

    internal static void SetCurrentResource(TechType techType)
    {
        _currentTechType = techType;
        _currentTechName = techType == TechType.None ? "None" : Language.main.Get(techType);
    }

    internal static bool ShouldUseMobileScanner()
    {
        return Enabled
            && _currentTechType != TechType.None
            && MobileResourceScannerItem.IsEquipped()
            && TryGetHeldScanner(out var scanner)
            && scanner.energyMixin != null
            && scanner.energyMixin.charge > 0f;
    }

    internal static int GetScannerCountFallback(int count)
    {
        if (!Enabled || count > 0)
            return count;

        if (!MobileResourceScannerItem.IsEquipped())
            return 0;

        return TryGetHeldScanner(out var scanner) && scanner.energyMixin != null && scanner.energyMixin.charge > 0f
            ? 1
            : 0;
    }

    internal static void GatherMobileNodes(uGUI_ResourceTracker tracker, HashSet<ResourceTrackerDatabase.ResourceInfo> nodes, List<TechType> techTypes)
    {
        var camera = MainCamera.camera;
        if (camera == null)
            return;

        nodes.Clear();
        techTypes.Clear();
        if (!TryConsumeScannerEnergy())
            return;

        var position = camera.transform.position;
        var range = Config.MobileResourceScannerRangeMeters;
        ResourceTrackerDatabase.GetNodes(position, range, _currentTechType, nodes);

        if (_activeIntervalSeconds == Config.MobileResourceScannerIntervalSeconds)
            return;

        tracker.CancelInvoke("GatherNodes");
        tracker.InvokeRepeating("GatherNodes", Config.MobileResourceScannerIntervalSeconds, Config.MobileResourceScannerIntervalSeconds);
        _activeIntervalSeconds = Config.MobileResourceScannerIntervalSeconds;
    }

    internal static void ShowMenu()
    {
        if (!Enabled)
            return;

        if (!TryGetHeldScanner(out _))
        {
            if (!IsLeftMouseMenuBinding())
                ErrorMessage.AddWarning("Equip the Scanner to use the mobile scanner menu.");

            return;
        }

        CloseMenu();

        MenuGameObject = new GameObject("InferiusMobileResourceScannerMenu");
        MenuGameObject.transform.SetParent(uGUI.main.hud.transform, false);
        var rootRect = MenuGameObject.AddComponent<RectTransform>();
        SetupRect(rootRect, new Vector2(MenuWidth, MenuHeight), Vector2.zero);

        var panel = CreatePanel(MenuGameObject.transform, "Panel", new Vector2(MenuWidth, MenuHeight), Vector2.zero, PanelColor);
        var panelImage = panel.GetComponent<Image>();
        panelImage.raycastTarget = true;

        var menu = MenuGameObject.AddComponent<MobileResourceScannerMenu>();
        menu.Select();

        CreatePanel(panel.transform, "HeaderBand", new Vector2(MenuWidth, 82), new Vector2(0, 319), HeaderColor);
        CreateIcon(panel.transform, "ScannerIcon", TechType.Scanner, new Vector2(56, 56), new Vector2(-358, 322), new Color(0.8f, 1f, 1f, 0.92f));
        CreateText(panel.transform, "Title", "Mobile Resource Scanner", 26f, FontStyles.Bold, TextAlignmentOptions.Left, BrightTextColor, new Vector2(420, 36), new Vector2(-88, 337));
        CreateText(panel.transform, "Subtitle", "Select a nearby resource signature", 14f, FontStyles.Normal, TextAlignmentOptions.Left, MutedTextColor, new Vector2(420, 26), new Vector2(-88, 307));

        CreateText(panel.transform, "ActiveLabel", "Tracking: " + _currentTechName, 15f, FontStyles.Bold, TextAlignmentOptions.Right, BrightTextColor, new Vector2(260, 30), new Vector2(258, 330));
        CreateText(panel.transform, "RangeLabel", $"{Config.MobileResourceScannerRangeMeters} m | {Config.MobileResourceScannerIntervalSeconds} sec refresh", 12f, FontStyles.Normal, TextAlignmentOptions.Right, MutedTextColor, new Vector2(260, 24), new Vector2(258, 306));
        CreatePanel(panel.transform, "Divider", new Vector2(MenuWidth - 64, 2), new Vector2(0, 274), DividerColor);

        var input = CreateFilterInput(panel.transform, new Vector2(MenuWidth - 120, 48), new Vector2(0, 231));

        var techs = GetAvailableTechTypes();
        if (techs.Count < 1)
        {
            ErrorMessage.AddWarning("No mobile scanner resources found.");
            CloseMenu();
            return;
        }

        techs.Insert(0, TechType.None);

        var scrollObject = CreatePanel(panel.transform, "ScrollView", new Vector2(MenuWidth - 88, 498), new Vector2(0, -38), ScrollColor);
        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();

        var viewport = CreatePanel(scrollObject.transform, "Viewport", scrollRectTransform.sizeDelta, Vector2.zero, new Color(1f, 1f, 1f, 0.02f));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var gridContent = new GameObject("ResourceGrid");
        gridContent.transform.SetParent(viewport.transform, false);
        var gridRect = gridContent.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(18f, -18f);
        gridRect.sizeDelta = new Vector2(MenuWidth - 124, CalculateGridHeight(techs.Count));

        var layout = gridContent.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(TileWidth, TileHeight);
        layout.spacing = new Vector2(TileSpacing, TileSpacing);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = TileColumns;

        var scannerGrid = gridContent.AddComponent<MobileResourceScannerGrid>();
        scannerGrid.Columns = TileColumns;

        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.content = gridRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.horizontal = false;
        scrollRect.viewport = viewportRect;
        scrollRect.scrollSensitivity = 48f;

        foreach (var techType in techs)
        {
            var buttonObject = CreateResourceTile(gridContent.transform, scannerGrid, techType);
            SetupButton(buttonObject, techType);
        }

        scrollRect.verticalNormalizedPosition = 1;
        scrollRect.horizontalNormalizedPosition = 0;

        var textInput = input.GetComponentInChildren<TMP_InputField>();
        if (textInput != null)
        {
            textInput.text = "";
            textInput.onValueChanged = new TMP_InputField.OnChangeEvent();
            textInput.onValueChanged.AddListener(value => FilterMenuEntries(gridContent, value));
        }

        scannerGrid.RefreshVisibleItems();
        scannerGrid.SelectFirstItem();
        GamepadInputModule.current?.SetCurrentGrid(scannerGrid);
    }

    internal static void CloseMenu()
    {
        if (MenuGameObject == null)
            return;

        try
        {
            _closingMenu = true;
            var menu = MenuGameObject.GetComponent<MobileResourceScannerMenu>();
            if (menu != null)
                menu.Deselect();

            UnityEngine.Object.Destroy(MenuGameObject);
            MenuGameObject = null;
        }
        finally
        {
            _closingMenu = false;
        }
    }

    internal static void OnMenuDeselected()
    {
        if (_closingMenu || MenuGameObject == null)
            return;

        CloseMenu();
    }

    private static void RestoreCurrentResource()
    {
        if (!Enum.TryParse(Config.MobileResourceScannerCurrentResource, out TechType techType))
            techType = TechType.None;

        SetCurrentResource(techType);
    }

    private static bool IsMenuHotkeyDown()
    {
        if (!GameInput.GetButtonDown(MobileResourceScannerInput.OpenMenu))
            return false;

        return IsConfiguredModifierHeld();
    }

    private static bool IsConfiguredModifierHeld()
    {
        return Config.MobileResourceScannerMenuModifier switch
        {
            "None" => true,
            "Ctrl" => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            "Alt" => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            "LeftShift" => Input.GetKey(KeyCode.LeftShift),
            "RightShift" => Input.GetKey(KeyCode.RightShift),
            "LeftCtrl" => Input.GetKey(KeyCode.LeftControl),
            "RightCtrl" => Input.GetKey(KeyCode.RightControl),
            "LeftAlt" => Input.GetKey(KeyCode.LeftAlt),
            "RightAlt" => Input.GetKey(KeyCode.RightAlt),
            _ => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
        };
    }

    private static void UpdateScannerReticleHint(ScannerTool scanner)
    {
        if (!Enabled
            || MenuGameObject != null
            || HandReticle.main == null
            || scanner == null
            || !scanner.isDrawn
            || MobileResourceScannerInput.OpenMenu == GameInput.Button.None
            || !MobileResourceScannerItem.IsEquipped())
        {
            return;
        }

        var formattedButton = FormatMenuBinding();
        if (string.IsNullOrEmpty(formattedButton))
            return;

        HandReticle.main.SetTextRaw(HandReticle.TextType.UseSubscript, $"Press {formattedButton} to select resource");
    }

    private static string FormatMenuBinding()
    {
        var button = GameInput.FormatButton(MobileResourceScannerInput.OpenMenu, false);
        var modifier = Config.MobileResourceScannerMenuModifier;
        if (string.IsNullOrEmpty(modifier) || modifier == "None")
            return button;

        return $"{GetModifierDisplayName(modifier)} + {button}";
    }

    private static bool IsLeftMouseMenuBinding()
    {
        if (!GameInput.IsInitialized || MobileResourceScannerInput.OpenMenu == GameInput.Button.None)
            return false;

        return IsLeftMouseBinding(GameInput.GetBinding(GameInput.Device.Keyboard, MobileResourceScannerInput.OpenMenu, GameInput.BindingSet.Primary))
            || IsLeftMouseBinding(GameInput.GetBinding(GameInput.Device.Keyboard, MobileResourceScannerInput.OpenMenu, GameInput.BindingSet.Secondary));
    }

    private static bool IsLeftMouseBinding(string binding)
    {
        return string.Equals(binding, "<Mouse>/leftButton", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetModifierDisplayName(string modifier)
    {
        return modifier switch
        {
            "LeftShift" => "Left Shift",
            "RightShift" => "Right Shift",
            "LeftCtrl" => "Left Ctrl",
            "RightCtrl" => "Right Ctrl",
            "LeftAlt" => "Left Alt",
            "RightAlt" => "Right Alt",
            _ => modifier,
        };
    }

    private static bool TryGetHeldScanner(out ScannerTool scanner)
    {
        scanner = null!;
        var tool = Inventory.main?.GetHeldTool();
        if (tool == null)
            return false;

        scanner = tool as ScannerTool ?? tool.GetComponent<ScannerTool>();
        return scanner != null && scanner.isDrawn;
    }

    private static bool TryConsumeScannerEnergy()
    {
        if (!TryGetHeldScanner(out var scanner) || scanner.energyMixin == null)
            return false;

        var energyMultiplier = Mathf.Clamp(Config.MobileResourceScannerEnergyUsePercent, 0, 200) / 100f;
        var amount = Mathf.Max(0f, scanner.powerConsumption * Mathf.Max(1, Config.MobileResourceScannerIntervalSeconds) * energyMultiplier);
        if (amount <= 0f || scanner.energyMixin.ConsumeEnergy(amount))
            return true;

        if (Time.time >= _nextPowerWarningTime)
        {
            ErrorMessage.AddWarning("Scanner battery depleted.");
            _nextPowerWarningTime = Time.time + 5f;
        }

        return false;
    }

    private static List<TechType> GetAvailableTechTypes()
    {
        var techs = new List<TechType>();
        if (Config.MobileResourceScannerShowAllTechTypes)
        {
            foreach (TechType techType in Enum.GetValues(typeof(TechType)))
            {
                if (IsAllowedTechType(techType))
                    techs.Add(techType);
            }
        }
        else
        {
            foreach (var techType in ResourceTrackerDatabase.GetTechTypes())
            {
                if (IsAllowedTechType(techType))
                    techs.Add(techType);
            }
        }

        techs.Sort((a, b) => string.Compare(Language.main.Get(a), Language.main.Get(b), StringComparison.Ordinal));
        return techs;
    }

    private static bool IsAllowedTechType(TechType techType)
    {
        return techType != TechType.None
            && (!Config.MobileResourceScannerRequireScanned || PDAScanner.ContainsCompleteEntry(techType));
    }

    private static void FilterMenuEntries(GameObject gridGameObject, string value)
    {
        var lower = (value ?? string.Empty).ToLowerInvariant();
        var count = 0;
        foreach (var tile in gridGameObject.GetComponentsInChildren<MobileResourceScannerTile>(true))
        {
            var active = value == null || value.Length < 2 || tile.MatchesFilter(lower);
            tile.gameObject.SetActive(active);
            if (active)
                count++;
        }

        var rect = gridGameObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, CalculateGridHeight(count));

        var grid = gridGameObject.GetComponent<MobileResourceScannerGrid>();
        if (grid != null)
            grid.RefreshVisibleItems();
    }

    private static void SetupButton(GameObject gameObject, TechType techType)
    {
        gameObject.name = $"Button{techType}";
        var button = gameObject.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() =>
        {
            SetCurrentResource(techType);
            Config.MobileResourceScannerCurrentResource = techType.ToString();
            Config.Save();
            ErrorMessage.AddWarning($"Mobile scanner resource set to {_currentTechName}");
            CloseMenu();
        });

        var tile = gameObject.GetComponent<MobileResourceScannerTile>();
        if (tile != null)
            tile.SetCurrent(techType == _currentTechType);
    }

    private static GameObject CreateResourceTile(Transform parent, MobileResourceScannerGrid grid, TechType techType)
    {
        var tileObject = new GameObject($"Tile{techType}");
        tileObject.transform.SetParent(parent, false);

        var rect = tileObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(TileWidth, TileHeight);

        var background = tileObject.AddComponent<Image>();
        background.color = MobileResourceScannerTile.NormalColor;

        var button = tileObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.94f, 1f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var accent = CreatePanel(tileObject.transform, "CurrentAccent", new Vector2(5, TileHeight - 16), new Vector2(-TileWidth / 2f + 8f, 0), new Color(1f, 0.76f, 0.26f, 0.95f)).GetComponent<Image>();
        accent.gameObject.SetActive(false);

        var icon = CreateIcon(tileObject.transform, "Icon", techType, new Vector2(42, 42), new Vector2(0, 18), Color.white);
        if (techType == TechType.None)
        {
            icon.gameObject.SetActive(false);
            CreateText(tileObject.transform, "OffGlyph", "OFF", 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.42f, 0.96f), new Vector2(64, 32), new Vector2(0, 18));
        }

        var label = CreateText(
            tileObject.transform,
            "Label",
            techType == TechType.None ? "None" : Language.main.Get(techType),
            13f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            BrightTextColor,
            new Vector2(TileWidth - 18, 34),
            new Vector2(0, -29));
        label.enableAutoSizing = true;
        label.fontSizeMin = 9f;
        label.fontSizeMax = 13f;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var tile = tileObject.AddComponent<MobileResourceScannerTile>();
        tile.Initialize(techType, button, background, accent, icon, label);
        grid.Register(tile);

        return tileObject;
    }

    private static GameObject CreateFilterInput(Transform parent, Vector2 size, Vector2 position)
    {
        var input = UnityEngine.Object.Instantiate(uGUI.main.userInput.inputField.gameObject, parent);
        input.name = "FilterInput";
        var inputRect = input.GetComponent<RectTransform>();
        SetupRect(inputRect, size, position);

        foreach (var image in input.GetComponentsInChildren<Image>(true))
            image.color = new Color(0.02f, 0.12f, 0.16f, 0.86f);

        foreach (var text in input.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.color = BrightTextColor;
            text.fontSize = 15f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        var textInput = input.GetComponentInChildren<TMP_InputField>();
        if (textInput != null && textInput.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.text = "Filter resources";
            placeholder.color = new Color(0.68f, 0.86f, 0.88f, 0.55f);
        }

        return input;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        SetupRect(gameObject.AddComponent<RectTransform>(), size, position);
        var image = gameObject.AddComponent<Image>();
        image.color = color;
        return gameObject;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color, Vector2 rectSize, Vector2 position)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        SetupRect(gameObject.AddComponent<RectTransform>(), rectSize, position);

        var text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static uGUI_Icon CreateIcon(Transform parent, string name, TechType techType, Vector2 size, Vector2 position, Color color)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        SetupRect(gameObject.AddComponent<RectTransform>(), size, position);

        var icon = gameObject.AddComponent<uGUI_Icon>();
        icon.color = color;
        icon.raycastTarget = false;

        if (techType != TechType.None)
        {
            icon.sprite = SpriteManager.Get(techType, null);
            icon.enabled = icon.sprite != null;
            ScannerRoom.DrillableScanFeature.ApplyScannerIcon(icon, techType);
        }
        else
        {
            icon.enabled = false;
        }

        return icon;
    }

    private static void SetupRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static int CalculateGridHeight(int itemCount)
    {
        if (itemCount <= 0)
            return TileHeight;

        var rows = Mathf.CeilToInt(itemCount / (float)TileColumns);
        return rows * TileHeight + Mathf.Max(0, rows - 1) * TileSpacing + 36;
    }

    [HarmonyPatch(typeof(uGUI_ResourceTracker), "IsVisibleNow")]
    private static class ResourceTrackerIsVisibleNowPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var target = AccessTools.Method(typeof(Equipment), nameof(Equipment.GetCount));
            var replacement = AccessTools.Method(typeof(MobileResourceScannerFeature), nameof(GetScannerCountFallback));

            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt
                    && codes[i].operand is MethodInfo method
                    && method == target)
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, replacement));
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(uGUI_ResourceTracker), "GatherScanned")]
    private static class ResourceTrackerGatherScannedPatch
    {
        public static bool Prefix(uGUI_ResourceTracker __instance)
        {
            if (!Enabled || !MobileResourceScannerItem.IsEquipped() || _currentTechType == TechType.None)
                return true;

            if (!ShouldUseMobileScanner())
                return true;

            AccessTools.Method(typeof(uGUI_ResourceTracker), "GatherNodes")?.Invoke(__instance, Array.Empty<object>());
            return false;
        }
    }

    [HarmonyPatch(typeof(uGUI_ResourceTracker), "GatherNodes")]
    private static class ResourceTrackerGatherNodesPatch
    {
        public static bool Prefix(uGUI_ResourceTracker __instance, HashSet<ResourceTrackerDatabase.ResourceInfo> ___nodes, List<TechType> ___techTypes)
        {
            if (!ShouldUseMobileScanner())
                return true;

            GatherMobileNodes(__instance, ___nodes, ___techTypes);
            return false;
        }
    }

    [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Open))]
    private static class IngameMenuOpenPatch
    {
        public static bool Prefix()
        {
            if (MenuGameObject == null)
                return true;

            CloseMenu();
            GameInput.ClearInput();
            return false;
        }
    }

    [HarmonyPatch(typeof(ScannerTool), nameof(ScannerTool.Update))]
    private static class ScannerToolUpdatePatch
    {
        public static void Postfix(ScannerTool __instance)
        {
            UpdateScannerReticleHint(__instance);
        }
    }
}
