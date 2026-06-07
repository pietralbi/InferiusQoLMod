namespace InferiusQoL.Features.AutoCraft;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Nearby fabricators (Fabricator, Workbench, ...) used to:
/// 1. recursively determine whether a recipe is craftable during auto-craft
/// 2. calculate energy consumption for batch crafting across multiple fabricators
/// </summary>
public static class ClosestFabricators
{
    private const float CACHE_DURATION = 0.5f;
    private static float _cacheExpired = 0f;
    private static GhostCrafter[] _cached = new GhostCrafter[0];
    private static readonly Dictionary<CraftTree.Type, List<TechType>> _craftable = new Dictionary<CraftTree.Type, List<TechType>>();

    public static GhostCrafter[] Fabricators
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

    public static void Add(GhostCrafter crafter)
    {
        if (_cached.Any(x => x == crafter)) return;
        Array.Resize(ref _cached, _cached.Length + 1);
        _cached[_cached.Length - 1] = crafter;
    }

    private static GhostCrafter[] Find()
    {
        var result = new List<GhostCrafter>();
        GhostCrafter[] found = new GhostCrafter[0];

        var useStorage = AutoCraftSettings.UseStorage;
        if (useStorage == NeighboringStorage.Off) return result.ToArray();

        if (useStorage == NeighboringStorage.Inside && Player.main != null && Player.main.IsInside())
        {
            if (Player.main.currentEscapePod != null)
                found = Player.main.currentEscapePod.GetComponentsInChildren<GhostCrafter>();
            else if (Player.main.IsInSub() && Player.main.currentSub != null)
                found = Player.main.currentSub.GetComponentsInChildren<GhostCrafter>();
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
                    found = EscapePod.main.GetComponentsInChildren<GhostCrafter>();
            }
            foreach (var vehicle in UnityEngine.Object.FindObjectsOfType<Vehicle>())
            {
                if (vehicle?.liveMixin != null && vehicle.liveMixin.IsAlive() && vehicle.GetDistanceToPlayer() < rangeM)
                    found = found.Concat(vehicle.GetComponentsInChildren<GhostCrafter>()).ToArray();
            }
            foreach (var subRoot in UnityEngine.Object.FindObjectsOfType<SubRoot>())
            {
                if (subRoot != null && subRoot.GetDistanceToPlayer() < rangeM)
                    found = found.Concat(subRoot.GetComponentsInChildren<GhostCrafter>()).ToArray();
            }
        }

        foreach (var gc in found)
        {
            if (gc == null) continue;
            if (!AutoCraftMain.IsGhostCrafterCraftTree(gc.craftTree)) continue;
            var constructable = gc.GetComponent<Constructable>();
            if (constructable != null && !constructable.constructed) continue;
            result.Add(gc);
        }

        var playerPosFinal = Player.main != null ? Player.main.transform.position : Vector3.zero;
        return result
            .OrderBy(x => (playerPosFinal - x.transform.position).sqrMagnitude)
            .ToArray();
    }

    private static void GenerateTechTypeList()
    {
        _craftable.Clear();
        foreach (CraftTree.Type type in Enum.GetValues(typeof(CraftTree.Type)))
        {
            if (!AutoCraftMain.IsGhostCrafterCraftTree(type)) continue;
            var list = new List<TechType>();
            var tree = CraftTree.GetTree(type);
            if (tree?.nodes == null) continue;
            using (var e = tree.nodes.Traverse(true))
            {
                while (e.MoveNext())
                {
                    var node = e.Current;
                    if (node != null && node.action == TreeAction.Craft)
                        list.Add(node.techType0);
                }
            }
            _craftable[type] = list;
        }
    }

    public static bool CanCraft(TechType techType)
    {
        if (_craftable.Count == 0) GenerateTechTypeList();
        foreach (var fab in Fabricators)
        {
            if (_craftable.TryGetValue(fab.craftTree, out var list) && list.Contains(techType))
                return true;
        }
        return false;
    }

    public static bool HasEnergy(Dictionary<TechType, int> crafted, out float needEnergy)
    {
        if (_craftable.Count == 0) GenerateTechTypeList();
        needEnergy = 0f;
        var relayConsumed = new Dictionary<PowerRelay, float>();
        var missing = new Dictionary<TechType, float>();

        foreach (var kvp in crafted)
        {
            float recipeEnergyCost = AutoCraftMain.DefaultRecipeEnergyCost;
            TechData.GetEnergyCost(kvp.Key, out recipeEnergyCost);
            if (recipeEnergyCost <= 0) recipeEnergyCost = AutoCraftMain.DefaultRecipeEnergyCost;
            float remaining = recipeEnergyCost * kvp.Value;

            foreach (var fab in Fabricators)
            {
                if (!_craftable.TryGetValue(fab.craftTree, out var list) || !list.Contains(kvp.Key)) continue;
                var relay = fab.GetPowerRelay();
                if (relay == null) continue;
                relayConsumed.TryGetValue(relay, out var already);
                float avail = relay.GetPower() - already;
                if (avail >= remaining)
                {
                    relayConsumed.Inc(relay, remaining);
                    remaining = 0f;
                }
                else if (avail > 0f)
                {
                    relayConsumed.Inc(relay, avail);
                    remaining -= avail;
                }
                if (remaining <= 0f) break;
            }
            if (remaining > 0f) missing.Inc(kvp.Key, remaining);
        }
        needEnergy = missing.Sum(x => x.Value);
        return needEnergy <= 0f;
    }

    public static bool ConsumeEnergy(Dictionary<TechType, int> crafted, out float consumedTotal)
    {
        consumedTotal = 0f;
        foreach (var kvp in crafted)
        {
            float recipeEnergyCost = AutoCraftMain.DefaultRecipeEnergyCost;
            TechData.GetEnergyCost(kvp.Key, out recipeEnergyCost);
            if (recipeEnergyCost <= 0) recipeEnergyCost = AutoCraftMain.DefaultRecipeEnergyCost;
            float remaining = recipeEnergyCost * kvp.Value;

            foreach (var fab in Fabricators)
            {
                if (!_craftable.TryGetValue(fab.craftTree, out var list) || !list.Contains(kvp.Key)) continue;
                var relay = fab.GetPowerRelay();
                if (relay == null) continue;
                float wanted = Mathf.Min(relay.GetPower(), remaining);
                float consumed;
                PowerSystem.ConsumeEnergy(relay, wanted, out consumed);
                remaining -= consumed;
                consumedTotal += consumed;
                if (remaining <= 0f) break;
            }
            if (remaining > 0f) return false;
        }
        return true;
    }
}
