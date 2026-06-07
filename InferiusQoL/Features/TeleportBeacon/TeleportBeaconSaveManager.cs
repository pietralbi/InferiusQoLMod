namespace InferiusQoL.Features.TeleportBeacon;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using InferiusQoL.Logging;
using Newtonsoft.Json;

/// <summary>
/// Persistence for per-beacon data: name, efficiency tier. Key = UniqueIdentifier.Id,
/// stable across save/load and unique per instance.
///
/// Serialized to beacons.json beside the DLL. Global across all save games, but
/// UniqueIdentifier.Id is unique per base build, so it does not collide.
/// </summary>
public static class TeleportBeaconSaveManager
{
    private static readonly Dictionary<string, BeaconData> _data = new Dictionary<string, BeaconData>();
    private static bool _loaded = false;

    public static void Load()
    {
        _data.Clear();
        _loaded = true;

        var path = GetSavePath();
        if (path == null) return;

        if (!File.Exists(path))
        {
            QoLLog.Info(Category.Teleport, $"No beacon save found (fresh state)");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonConvert.DeserializeObject<List<BeaconData>>(json);
            if (list != null)
            {
                foreach (var d in list)
                {
                    if (!string.IsNullOrEmpty(d?.id))
                        _data[d.id] = d;
                }
            }
            QoLLog.Info(Category.Teleport, $"Loaded {_data.Count} beacon records");
        }
        catch (Exception ex)
        {
            QoLLog.Error(Category.Teleport, $"Failed to load beacons.json: {ex.Message}", ex);
        }
    }

    public static void Save()
    {
        if (!_loaded) return;
        var path = GetSavePath();
        if (path == null) return;

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var list = new List<BeaconData>(_data.Values);
            File.WriteAllText(path, JsonConvert.SerializeObject(list, Formatting.Indented));
        }
        catch (Exception ex)
        {
            QoLLog.Error(Category.Teleport, $"Failed to save beacons.json: {ex.Message}", ex);
        }
    }

    public static BeaconData GetOrCreate(string id)
    {
        if (string.IsNullOrEmpty(id)) return new BeaconData { id = "" };
        if (!_data.TryGetValue(id, out var d))
        {
            d = new BeaconData { id = id, name = "Beacon", efficiencyTier = 0 };
            _data[id] = d;
        }
        return d;
    }

    public static void Update(BeaconData data)
    {
        if (string.IsNullOrEmpty(data?.id)) return;
        _data[data.id] = data;
        Save();
    }

    private static string? GetSavePath()
    {
        var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dllDir)) return null;
        return Path.Combine(dllDir!, "beacons.json");
    }

    public sealed class BeaconData
    {
        public string id = "";
        public string name = "Beacon";
        public int efficiencyTier = 0; // 0 = none, 1-3 = chip tiers
    }
}
