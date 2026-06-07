#nullable enable
namespace InferiusQoL.Features.ScrollableInventory;

using System.Collections;
using System.IO;
using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;
using UnityEngine.UI;

public static class ScrollableInventoryFeature
{
    private const string BundleFileName = "ScrollableInventory.bundle";
    private const string BundleName = "advancedinventory";
    private const string ScrollViewAssetName = "ScrollView";
    private const string GridAssetName = "grid";
    private const string ScrollViewObjectName = "InferiusQoL_ScrollableInventory";
    private const int Offset = 2;
    private const int ScrollbarWidth = 20;

    private static AssetBundle? _assets;
    private static GameObject? _scrollPrefab;
    private static Texture2D? _gridTexture;
    private static bool _loadAttempted;
    private static bool _loadErrorLogged;
    private static bool _startedLogged;

    public static void Refresh(uGUI_ItemsContainer guiContainer, bool resetScrollPosition = false)
    {
        if (!IsActive()) return;
        if (!IsSupportedContainer(guiContainer)) return;

        var container = guiContainer.container;
        if (container == null) return;

        var scrollRect = GetOwnedScrollRect(guiContainer);
        if (scrollRect == null && container.sizeY <= MaxVisibleRows)
            return;

        if (!EnsureAssetsLoaded())
            return;

        if (guiContainer.grid != null && _gridTexture != null)
        {
            guiContainer.grid.texture = _gridTexture;
        }

        var created = false;
        if (scrollRect == null)
        {
            scrollRect = CreateScrollView(guiContainer);
            created = scrollRect != null;
            if (scrollRect == null) return;
        }
        else
        {
            scrollRect.gameObject.SetActive(true);
        }

        UpdateScrollView(guiContainer, container, scrollRect, resetScrollPosition || created);
        LogStartedOnce();
    }

    public static IEnumerator DeferredRefresh(uGUI_ItemsContainer guiContainer)
    {
        yield return new WaitForEndOfFrame();
        Refresh(guiContainer);
    }

    public static void Disable(uGUI_ItemsContainer guiContainer)
    {
        var scrollRect = GetOwnedScrollRect(guiContainer);
        if (scrollRect != null)
        {
            scrollRect.gameObject.SetActive(false);
        }
    }

    public static void ApplyRuntime()
    {
        foreach (var guiContainer in Resources.FindObjectsOfTypeAll<uGUI_ItemsContainer>())
        {
            if (guiContainer == null) continue;
            if (string.IsNullOrEmpty(guiContainer.gameObject.scene.name)) continue;
            Refresh(guiContainer);
        }
    }

    private static bool IsActive()
    {
        return !Plugin.HasAdvancedInventory;
    }

    private static int MaxVisibleRows => Mathf.Clamp(
        InferiusConfig.Instance.ScrollableInventoryMaxVisibleRows,
        4,
        16);

    private static bool IsSupportedContainer(uGUI_ItemsContainer guiContainer)
    {
        if (guiContainer == null || guiContainer.inventory == null) return false;
        if (ReferenceEquals(guiContainer, guiContainer.inventory.inventory)) return true;
        if (ReferenceEquals(guiContainer, guiContainer.inventory.storage)) return true;

        var torpedoStorage = guiContainer.inventory.torpedoStorage;
        if (torpedoStorage == null) return false;

        foreach (var storage in torpedoStorage)
        {
            if (ReferenceEquals(guiContainer, storage)) return true;
        }

        return false;
    }

    private static bool EnsureAssetsLoaded()
    {
        if (_scrollPrefab != null && _gridTexture != null)
            return true;

        if (!_loadAttempted)
        {
            _loadAttempted = true;
            _assets = FindLoadedBundle() ?? AssetBundle.LoadFromFile(Path.Combine(ModDirectory, "Assets", BundleFileName));
            if (_assets != null)
            {
                _scrollPrefab = _assets.LoadAsset<GameObject>(ScrollViewAssetName);
                _gridTexture = _assets.LoadAsset<Texture2D>(GridAssetName);
            }
        }

        if (_scrollPrefab != null && _gridTexture != null)
            return true;

        if (!_loadErrorLogged)
        {
            _loadErrorLogged = true;
            QoLLog.Warning(Category.Inventory,
                $"ScrollableInventory asset bundle missing or incomplete. Expected 'Assets/{BundleFileName}' beside InferiusQoL.dll.");
        }
        return false;
    }

    private static AssetBundle? FindLoadedBundle()
    {
        foreach (var bundle in Resources.FindObjectsOfTypeAll<AssetBundle>())
        {
            if (bundle != null && bundle.name == BundleName)
                return bundle;
        }
        return null;
    }

    private static string ModDirectory =>
        Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? ".";

    private static ScrollRect? CreateScrollView(uGUI_ItemsContainer guiContainer)
    {
        if (_scrollPrefab == null) return null;

        var originalParent = guiContainer.rectTransform.parent;
        var originalSibling = guiContainer.rectTransform.GetSiblingIndex();
        var scrollObject = Object.Instantiate(_scrollPrefab, originalParent);
        scrollObject.name = ScrollViewObjectName;
        scrollObject.transform.SetSiblingIndex(originalSibling);

        var scrollRect = scrollObject.GetComponent<ScrollRect>();
        var scrollTransform = scrollObject.transform as RectTransform;
        if (scrollRect == null || scrollTransform == null || scrollRect.content == null)
        {
            Object.Destroy(scrollObject);
            QoLLog.Warning(Category.Inventory, "ScrollableInventory ScrollView prefab is missing required UI components.");
            return null;
        }

        var contentParent = scrollRect.content.childCount > 0
            ? scrollRect.content.GetChild(0)
            : scrollRect.content;
        guiContainer.rectTransform.SetParent(contentParent, false);

        scrollTransform.pivot = guiContainer.rectTransform.pivot;
        scrollTransform.anchorMin = guiContainer.rectTransform.anchorMin;
        scrollTransform.anchorMax = guiContainer.rectTransform.anchorMax;
        scrollTransform.anchoredPosition = new Vector2(guiContainer.rectTransform.anchoredPosition.x, 0f);

        HideGridChildren(guiContainer);
        EnsureScrollbar(scrollRect);

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.verticalScrollbarSpacing = -ScrollbarWidth;
        scrollRect.scrollSensitivity = uGUI_ItemsContainer.CellHeight;
        return scrollRect;
    }

    private static void HideGridChildren(uGUI_ItemsContainer guiContainer)
    {
        if (guiContainer.grid == null) return;

        foreach (Transform child in guiContainer.grid.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private static void EnsureScrollbar(ScrollRect scrollRect)
    {
        if (scrollRect.verticalScrollbar == null)
        {
            var source = GetPdaScrollbar();
            if (source != null)
            {
                scrollRect.verticalScrollbar = Object
                    .Instantiate(source.gameObject, scrollRect.transform)
                    .GetComponent<Scrollbar>();
            }
        }

        if (scrollRect.verticalScrollbar == null) return;

        var scrollbarTransform = scrollRect.verticalScrollbar.transform as RectTransform;
        if (scrollbarTransform == null) return;

        scrollbarTransform.anchorMin = new Vector2(1f, 0f);
        scrollbarTransform.anchorMax = new Vector2(1f, 1f);
        scrollbarTransform.pivot = new Vector2(1f, 1f);
        scrollbarTransform.sizeDelta = new Vector2(ScrollbarWidth, 0f);
        scrollbarTransform.anchoredPosition = Vector2.zero;
    }

    private static Scrollbar? GetPdaScrollbar()
    {
        var pda = Player.main?.GetPDA()?.ui;
        var encyclopedia = pda?.tabEncyclopedia as uGUI_EncyclopediaTab;
        return encyclopedia?.contentScrollRect?.verticalScrollbar;
    }

    private static void UpdateScrollView(
        uGUI_ItemsContainer guiContainer,
        ItemsContainer container,
        ScrollRect scrollRect,
        bool resetScrollPosition)
    {
        var visibleRows = Mathf.Min(container.sizeY, MaxVisibleRows);
        var size = guiContainer.rectTransform.sizeDelta;
        var scrollTransform = scrollRect.transform as RectTransform;
        if (scrollTransform == null) return;

        scrollTransform.sizeDelta = new Vector2(
            size.x + ScrollbarWidth + Offset,
            uGUI_ItemsContainer.CellHeight * visibleRows + Offset);
        scrollRect.content.sizeDelta = new Vector2(size.x + Offset, size.y + Offset);

        guiContainer.rectTransform.pivot = new Vector2(0.5f, 1f);
        guiContainer.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        guiContainer.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        guiContainer.rectTransform.anchoredPosition = Vector2.zero;

        if (resetScrollPosition)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private static ScrollRect? GetOwnedScrollRect(uGUI_ItemsContainer guiContainer)
    {
        var current = guiContainer.transform;
        while (current != null)
        {
            if (current.gameObject.name == ScrollViewObjectName)
                return current.GetComponent<ScrollRect>();
            current = current.parent;
        }
        return null;
    }

    private static void LogStartedOnce()
    {
        if (_startedLogged) return;
        _startedLogged = true;
        QoLLog.Info(Category.Inventory, $"ScrollableInventory active (max visible rows: {MaxVisibleRows})");
    }
}

[HarmonyPatch(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.Init))]
public static class ScrollableInventory_Init_Patch
{
    [HarmonyPostfix]
    public static void Postfix(uGUI_ItemsContainer __instance)
    {
        ScrollableInventoryFeature.Refresh(__instance, resetScrollPosition: true);
    }
}

[HarmonyPatch(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.OnResize))]
public static class ScrollableInventory_OnResize_Patch
{
    [HarmonyPostfix]
    public static void Postfix(uGUI_ItemsContainer __instance)
    {
        if (Player.main != null)
            Player.main.StartCoroutine(ScrollableInventoryFeature.DeferredRefresh(__instance));
        else
            ScrollableInventoryFeature.Refresh(__instance);
    }
}

[HarmonyPatch(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.Uninit))]
public static class ScrollableInventory_Uninit_Patch
{
    [HarmonyPostfix]
    public static void Postfix(uGUI_ItemsContainer __instance)
    {
        ScrollableInventoryFeature.Disable(__instance);
    }
}
