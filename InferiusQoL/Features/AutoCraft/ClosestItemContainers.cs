namespace InferiusQoL.Features.AutoCraft;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// Cache of nearby ItemsContainers (Inventory + surrounding lockers). Determines
/// the scope from AutoCraftSettings.UseStorage (Off / Inside / Range100).
/// Caches for 0.5s so the scene is not iterated every frame.
/// </summary>
public static class ClosestItemContainers
{
    private const string ASTERISK = "*\u200B";
    private static float _cacheExpired = 0f;
    private const float CACHE_DURATION = 0.5f;
    private static ItemsContainer[] _cached = new ItemsContainer[0];

    public static ItemsContainer[] Containers
    {
        get
        {
            if (_cacheExpired < Time.unscaledTime || _cacheExpired > Time.unscaledTime + CACHE_DURATION)
            {
                _cached = Find();
                _cacheExpired = Time.unscaledTime + CACHE_DURATION;
            }
            return _cached;
        }
    }

    private static ItemsContainer[] Find()
    {
        var result = new List<ItemsContainer>();
        StorageContainer[] storages = new StorageContainer[0];

        if (Inventory.main?.container != null)
            result.Add(Inventory.main.container);

        var useStorage = AutoCraftSettings.UseStorage;
        if (useStorage == NeighboringStorage.Off) return result.ToArray();

        if (useStorage == NeighboringStorage.Inside && Player.main != null && Player.main.IsInside())
        {
            if (Player.main.currentEscapePod != null)
                storages = Player.main.currentEscapePod.GetComponentsInChildren<StorageContainer>();
            else if (Player.main.IsInSub() && Player.main.currentSub != null)
                storages = Player.main.currentSub.GetComponentsInChildren<StorageContainer>();
        }
        else if (useStorage == NeighboringStorage.Range100)
        {
            var playerPos = Player.main != null ? Player.main.transform.position : Vector3.zero;
            var rangeM = AutoCraftSettings.RangeMeters;
            var rangeSq = AutoCraftSettings.RangeMetersSquared;

            if (EscapePod.main != null)
            {
                var diff = playerPos - EscapePod.main.transform.position;
                if (diff.sqrMagnitude < rangeSq)
                    storages = EscapePod.main.GetComponentsInChildren<StorageContainer>();
            }
            foreach (var vehicle in Object.FindObjectsOfType<Vehicle>())
            {
                if (vehicle == null) continue;
                if (vehicle.liveMixin != null && vehicle.liveMixin.IsAlive() && vehicle.GetDistanceToPlayer() < rangeM)
                    storages = storages.Concat(vehicle.GetComponentsInChildren<StorageContainer>()).ToArray();
            }
            foreach (var subRoot in Object.FindObjectsOfType<SubRoot>())
            {
                if (subRoot == null) continue;
                if (subRoot.GetDistanceToPlayer() < rangeM)
                    storages = storages.Concat(subRoot.GetComponentsInChildren<StorageContainer>()).ToArray();
            }
            foreach (var smallStorage in Object.FindObjectsOfType<SmallStorage>())
            {
                if (smallStorage == null) continue;
                var comp = smallStorage.GetComponent<StorageContainer>();
                if (comp == null || comp.container?.containerType != 0) continue;
                var diff = playerPos - smallStorage.transform.position;
                if (diff.sqrMagnitude < rangeSq)
                    result.Add(comp.container);
            }
        }

        foreach (var sc in storages)
        {
            if (sc?.container == null) continue;
            if (sc.container.containerType != 0) continue;
            if (sc.storageLabel != null && sc.storageLabel.StartsWith("Aquarium")) continue;

            // Asterisk-prefixed labels: filter through the label string. A storageLabel
            // ending with an asterisk means "do not use". Fallback without a TMPro reference.
            if (sc.storageLabel != null && sc.storageLabel.EndsWith(ASTERISK)) continue;

            var constructable = sc.GetComponent<Constructable>();
            if (constructable != null && !constructable.constructed) continue;

            result.Add(sc.container);
        }

        var playerPosFinal = Player.main != null ? Player.main.transform.position : Vector3.zero;
        return result
            .Distinct()
            .OrderBy(x => (playerPosFinal - x.tr.position).sqrMagnitude)
            .ToArray();
    }

    public static int GetPickupCount(TechType techType)
    {
        return Containers.Sum(c => c.GetCount(techType));
    }

    public static bool AddItem(TechType techType, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            var prefab = CraftData.InstantiateFromPrefab(null, techType, false);
            if (prefab == null) continue;
            var pickupable = prefab.GetComponent<Pickupable>();
            if (pickupable == null) continue;

            bool added = false;
            pickupable.Pickup(false);

            if (AutoCraftSettings.ReturnSurplus == ReturnSurplus.Lockers)
            {
                var field = typeof(ItemsContainer).GetField("_label",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    foreach (var container in Containers)
                    {
                        var label = field.GetValue(container) as string;
                        if (label == "Autosorter" && container.AddItem(pickupable) != null)
                        {
                            QoLLog.Debug(Category.AutoCraft, $"Add {techType} (Autosorter)");
                            added = true;
                            break;
                        }
                    }
                }
            }

            if (!added)
            {
                foreach (var container in Containers)
                {
                    if (AutoCraftSettings.ReturnSurplus == ReturnSurplus.Lockers
                        && container == Inventory.main?.container) continue;
                    if (container.AddItem(pickupable) != null)
                    {
                        QoLLog.Debug(Category.AutoCraft, $"Add {techType}");
                        added = true;
                        break;
                    }
                }
                // Fallback: inventory, even if it was skipped.
                if (AutoCraftSettings.ReturnSurplus == ReturnSurplus.Lockers && !added
                    && Inventory.main?.container?.AddItem(pickupable) != null)
                {
                    added = true;
                }
            }

            if (!added && Player.main != null)
            {
                pickupable.Drop(Player.main.transform.position + Vector3.down, Vector3.down, true);
                QoLLog.Debug(Category.AutoCraft, $"Drop {techType} (no room)");
            }
        }
        return true;
    }

    public static bool DestroyItem(TechType techType, int count = 1)
    {
        int removed = 0;
        foreach (var container in Containers)
        {
            for (int i = 0; i < count; i++)
            {
                if (container.DestroyItem(techType))
                {
                    removed++;
                    QoLLog.Trace(Category.AutoCraft, $"Remove {techType}");
                }
                if (removed == count) return true;
            }
        }
        QoLLog.Debug(Category.AutoCraft, $"Unable to remove {techType} (removed {removed}/{count})");
        return false;
    }
}
