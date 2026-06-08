namespace InferiusQoL.Features.InventoryViewer;

using System;
using System.Collections.Generic;
using System.Linq;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using InferiusQoL.UI;
using UnityEngine;

/// <summary>
/// Aggregate inventory viewer. Hotkey toggle (default I) -> IMGUI okno se
/// seznamem vsech polozek napric Inventory + StorageContainery (lockery,
/// carryally, vehicle storage) v dosahu. Per TechType count + zdroj (kolik
/// containeru).
///
/// Iteruje i inactive Pickupable containers (Carryally drzene v inventari)
/// pres Resources.FindObjectsOfTypeAll. Range filtruje containery dle
/// vzdalenosti od hrace (0 = bez limitu).
/// </summary>
public class InventoryViewerManager : MonoBehaviour
{
    private bool _open = false;
    private Vector2 _scroll = Vector2.zero;
    private string _filter = "";
    private float _lastToggleTime = -1f;
    private const float TOGGLE_COOLDOWN = 0.2f;

    private List<AggregateRow> _rows = new List<AggregateRow>();
    private float _lastScanTime = -1f;
    private const float SCAN_INTERVAL = 0.5f;
    private int _totalItems = 0;
    private int _totalContainers = 0;

    private Rect _windowRect = new Rect(60, 60, 520, 600);

    private void Update()
    {
        try
        {
            var cfg = InferiusConfig.Instance;
            if (!cfg.InventoryViewerEnabled) return;

            var player = Player.main;
            if (player == null) return;

            if (!Enum.TryParse<KeyCode>(cfg.InventoryViewerKey, true, out var key))
                key = KeyCode.I;

            if (!UnityEngine.Input.GetKeyDown(key)) return;
            if (HotkeyFocusGuard.ShouldIgnoreHotkey()) return;
            if (Time.time - _lastToggleTime < TOGGLE_COOLDOWN) return;
            _lastToggleTime = Time.time;

            _open = !_open;
            if (_open)
            {
                RefreshScan();
                // Subnautica re-locknul cursor kazdy frame pres FPS input modul.
                // UWE.Utils.lockCursor je flag ktery FPS modul respektuje -
                // pouziva ho i vanilla PDA + nas TeleportBeaconUI.
                UWE.Utils.lockCursor = false;
            }
            else
            {
                UWE.Utils.lockCursor = true;
            }
        }
        catch (Exception ex)
        {
            QoLLog.Error(Category.Inventory, "Update threw", ex);
        }
    }

    private void OnGUI()
    {
        if (!_open) return;
        if (!InferiusConfig.Instance.InventoryViewerEnabled) { _open = false; return; }

        // Periodic re-scan zatimco okno otevrene.
        if (Time.time - _lastScanTime > SCAN_INTERVAL) RefreshScan();

        _windowRect = GUILayout.Window(
            id: 0xCAFE01,
            screenRect: _windowRect,
            func: DrawWindow,
            text: $"Inventory Viewer  -  {_totalItems} items / {_totalContainers} containers");
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Filter:", GUILayout.Width(50));
        _filter = GUILayout.TextField(_filter ?? "", GUILayout.MinWidth(180));
        if (GUILayout.Button("Clear", GUILayout.Width(60))) _filter = "";
        if (GUILayout.Button("Refresh", GUILayout.Width(80))) RefreshScan();
        if (GUILayout.Button("Close", GUILayout.Width(60)))
        {
            _open = false;
            UWE.Utils.lockCursor = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        var filtered = string.IsNullOrEmpty(_filter)
            ? _rows
            : _rows.Where(r => r.DisplayName.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Item", GUILayout.MinWidth(280));
        GUILayout.Label("Count", GUILayout.Width(80));
        GUILayout.Label("In", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(440));
        foreach (var row in filtered)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(row.DisplayName, GUILayout.MinWidth(280));
            GUILayout.Label(row.Count.ToString(), GUILayout.Width(80));
            GUILayout.Label(row.ContainerCount + "x", GUILayout.Width(80));
            GUILayout.EndHorizontal();
        }
        if (filtered.Count == 0)
            GUILayout.Label("(no items match filter)");
        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void RefreshScan()
    {
        _lastScanTime = Time.time;
        try
        {
            var cfg = InferiusConfig.Instance;
            var counts = new Dictionary<TechType, (int total, HashSet<object> containers)>();

            // 1. Inventory hrace.
            if (cfg.InventoryViewerIncludePlayer)
            {
                var inv = Inventory.main;
                if (inv?.container != null)
                    AggregateContainer(inv.container, counts);
            }

            // 2. StorageContainers v dosahu (vc. inactive).
            var rangeM = cfg.InventoryViewerRangeMeters;
            var rangeSq = rangeM <= 0 ? float.MaxValue : (float)rangeM * rangeM;
            var playerPos = Player.main != null ? Player.main.transform.position : Vector3.zero;

            int containerCount = 0;
            var all = Resources.FindObjectsOfTypeAll<StorageContainer>()
                .Where(sc => sc != null && sc.gameObject != null
                             && !string.IsNullOrEmpty(sc.gameObject.scene.name));
            foreach (var sc in all)
            {
                if (sc?.container == null) continue;
                if (rangeSq < float.MaxValue)
                {
                    var diff = playerPos - sc.transform.position;
                    if (diff.sqrMagnitude > rangeSq) continue;
                }
                AggregateContainer(sc.container, counts);
                containerCount++;
            }

            // Build rows.
            _rows = counts
                .Select(kvp => new AggregateRow
                {
                    TechType = kvp.Key,
                    DisplayName = TryGetName(kvp.Key),
                    Count = kvp.Value.total,
                    ContainerCount = kvp.Value.containers.Count,
                })
                .OrderByDescending(r => r.Count)
                .ThenBy(r => r.DisplayName)
                .ToList();

            _totalItems = _rows.Sum(r => r.Count);
            _totalContainers = containerCount + (cfg.InventoryViewerIncludePlayer && Inventory.main != null ? 1 : 0);
        }
        catch (Exception ex)
        {
            QoLLog.Error(Category.Inventory, "RefreshScan threw", ex);
        }
    }

    private static void AggregateContainer(ItemsContainer c, Dictionary<TechType, (int total, HashSet<object> containers)> counts)
    {
        foreach (var invItem in c)
        {
            var p = invItem?.item;
            if (p == null) continue;
            var tt = p.GetTechType();
            if (tt == TechType.None) continue;
            if (!counts.TryGetValue(tt, out var cur))
            {
                cur = (0, new HashSet<object>());
                counts[tt] = cur;
            }
            cur.total++;
            cur.containers.Add(c);
            counts[tt] = cur;
        }
    }

    private static string TryGetName(TechType tt)
    {
        try
        {
            var lang = Language.main;
            if (lang != null)
            {
                var s = lang.Get(tt.AsString());
                if (!string.IsNullOrEmpty(s) && s != tt.AsString()) return s;
            }
        }
        catch { }
        return tt.AsString();
    }

    private struct AggregateRow
    {
        public TechType TechType;
        public string DisplayName;
        public int Count;
        public int ContainerCount;
    }
}
