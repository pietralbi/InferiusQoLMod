namespace InferiusQoL.Features.TeleportBeacon;

using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// IMGUI overlay for Teleport Beacon. Shown when clicking a beacon:
/// - Text input for naming the beacon
/// - List of all other beacons with name, distance, and cost
/// - Teleport / Close buttons
///
/// Uses Unity's OnGUI callback: quick, but visually simple.
/// </summary>
public class TeleportBeaconUI : MonoBehaviour
{
    private TeleportBeaconBehavior? _beacon;
    private bool _open = false;
    private Rect _windowRect = new Rect(200, 100, 500, 500);
    private Vector2 _scrollPosition = Vector2.zero;
    private string _nameEdit = "";
    private string _statusMessage = "";

    private void Awake()
    {
        _beacon = GetComponent<TeleportBeaconBehavior>();
    }

    public void Show()
    {
        if (_beacon == null) return;
        _open = true;
        _nameEdit = _beacon.Data.name;
        _statusMessage = "";
        _scrollPosition = Vector2.zero;

        UWE.Utils.lockCursor = false;
    }

    public void Hide()
    {
        _open = false;
        UWE.Utils.lockCursor = true;
    }

    private void OnGUI()
    {
        if (!_open || _beacon == null) return;

        GUI.skin.label.fontSize = 14;
        GUI.skin.button.fontSize = 13;
        GUI.skin.textField.fontSize = 14;

        _windowRect = GUILayout.Window(10_345_210, _windowRect, DrawWindow, "Teleport Beacon");
    }

    private void DrawWindow(int windowID)
    {
        if (_beacon == null) return;

        GUILayout.Label("Beacon name:");
        GUILayout.BeginHorizontal();
        _nameEdit = GUILayout.TextField(_nameEdit ?? "", GUILayout.Width(350));
        if (GUILayout.Button("Save", GUILayout.Width(80)))
        {
            var data = _beacon.Data;
            data.name = string.IsNullOrWhiteSpace(_nameEdit) ? "Beacon" : _nameEdit.Trim();
            TeleportBeaconSaveManager.Update(data);
            _statusMessage = "Name saved";
            QoLLog.Info(Category.Teleport, $"Renamed beacon {_beacon.Id} to '{data.name}'");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label($"Efficiency chip: {ChipSlotText()}");
        GUILayout.BeginHorizontal();
        if (_beacon.Data.efficiencyTier > 0)
        {
            if (GUILayout.Button("Remove chip", GUILayout.Width(120)))
                RemoveChip();
        }
        else
        {
            if (GUILayout.Button("Install best chip", GUILayout.Width(160)))
                InstallBestChip();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Destinations:");

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(280));

        var otherBeacons = TeleportBeaconBehavior.All;
        int otherCount = 0;
        foreach (var b in otherBeacons)
        {
            if (b == null || b == _beacon) continue;
            otherCount++;

            var dist = Vector3.Distance(_beacon.transform.position, b.transform.position);
            var cost = _beacon.EstimateCost(b);

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{b.Data.name}\n{dist:0}m, {cost:0} J", GUILayout.Width(330));
            if (GUILayout.Button("Teleport", GUILayout.Width(100), GUILayout.Height(40)))
            {
                if (_beacon.TryTeleportTo(b, out string reason))
                {
                    Hide();
                }
                else
                {
                    _statusMessage = "Failed: " + reason;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        if (otherCount == 0)
        {
            GUILayout.Label("No other beacons exist. Build another one elsewhere.");
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(_statusMessage))
            GUILayout.Label(_statusMessage);

        if (GUILayout.Button("Close", GUILayout.Height(30)))
        {
            Hide();
        }

        GUI.DragWindow();
    }

    private void OnDisable()
    {
        if (_open) Hide();
    }

    private string ChipSlotText()
    {
        if (_beacon == null) return "-";
        return _beacon.Data.efficiencyTier switch
        {
            1 => "MK1 (" + InferiusConfig.Instance.TeleportEfficiencyMK1Percent + "% of base cost)",
            2 => "MK2 (" + InferiusConfig.Instance.TeleportEfficiencyMK2Percent + "% of base cost)",
            3 => "MK3 (" + InferiusConfig.Instance.TeleportEfficiencyMK3Percent + "% of base cost)",
            _ => "empty",
        };
    }

    private void InstallBestChip()
    {
        if (_beacon == null) return;
        var inv = Inventory.main;
        if (inv?.container == null) return;

        var (tier, tt) = TeleportEfficiencyChips.FindHighestInInventory();
        if (tier == 0 || tt == TechType.None)
        {
            _statusMessage = "No efficiency chip in inventory";
            return;
        }

        // Find the Pickupable instance and remove it from inventory.
        var items = new System.Collections.Generic.List<InventoryItem>();
        foreach (var it in inv.container)
        {
            if (it?.item == null) continue;
            if (it.item.GetTechType() == tt)
            {
                items.Add(it);
                break;
            }
        }
        if (items.Count == 0)
        {
            _statusMessage = "Chip not found";
            return;
        }

        inv.container.RemoveItem(items[0].item, forced: true);
        UnityEngine.Object.Destroy(items[0].item.gameObject);

        var data = _beacon.Data;
        data.efficiencyTier = tier;
        TeleportBeaconSaveManager.Update(data);

        _statusMessage = $"Installed MK{tier} chip";
        InferiusQoL.Logging.QoLLog.Info(
            InferiusQoL.Logging.Category.Teleport,
            $"Beacon '{_beacon.Data.name}' installed efficiency chip MK{tier}");
    }

    private void RemoveChip()
    {
        if (_beacon == null) return;

        var tier = _beacon.Data.efficiencyTier;
        if (tier == 0)
        {
            _statusMessage = "No chip installed";
            return;
        }

        // Remove = destroy chip; the player crafts a new one if needed again.
        var data = _beacon.Data;
        data.efficiencyTier = 0;
        TeleportBeaconSaveManager.Update(data);

        _statusMessage = $"Removed MK{tier} chip (discarded)";
    }
}
