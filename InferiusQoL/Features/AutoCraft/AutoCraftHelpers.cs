namespace InferiusQoL.Features.AutoCraft;

using System;
using System.Collections.Generic;
using UnityEngine;

internal static class AutoCraftHelpers
{
    private static Transform? _searchOrigin;

    public static IDisposable PushSearchOrigin(Component? origin)
    {
        var previous = _searchOrigin;
        _searchOrigin = origin != null ? origin.transform : null;
        return new SearchOriginScope(previous);
    }

    public static Transform? SearchOriginTransform
    {
        get
        {
            if (_searchOrigin != null) return _searchOrigin;

            var menuClient = uGUI.main?.craftingMenu?.client as Component;
            if (menuClient != null) return menuClient.transform;

            return Player.main != null ? Player.main.transform : null;
        }
    }

    public static Vector3 SearchOriginPosition
    {
        get
        {
            var origin = SearchOriginTransform;
            return origin != null ? origin.position : Vector3.zero;
        }
    }

    public static void Inc<T>(this Dictionary<T, int> dict, T key, int value = 1)
    {
        dict.TryGetValue(key, out var cur);
        dict[key] = cur + value;
    }

    public static void Inc<T>(this Dictionary<T, float> dict, T key, float value = 1f)
    {
        dict.TryGetValue(key, out var cur);
        dict[key] = cur + value;
    }

    public static PowerRelay? GetPowerRelay(this GhostCrafter crafter)
    {
        if (crafter == null) return null;
        if (crafter.powerRelay != null) return crafter.powerRelay;
        return crafter.GetComponentInParent<PowerRelay>();
    }

    public static float GetDistanceTo(this BaseRoot baseRoot, Vector3 position)
    {
        if (baseRoot?.baseComp == null) return float.MaxValue;
        var grid = baseRoot.baseComp.WorldToGrid(position);
        int bestSq = int.MaxValue;
        Int3 bestCell = new Int3(int.MaxValue);
        foreach (var cell in baseRoot.baseComp.AllCells)
        {
            var diff = grid - cell;
            int sq = diff.SquareMagnitude();
            if (sq < bestSq) { bestSq = sq; bestCell = cell; }
        }
        Vector3 worldPos = baseRoot.baseComp.GridToWorld(bestCell);
        return (position - worldPos).magnitude;
    }

    public static float GetDistanceTo(this Vehicle vehicle, Vector3 position)
    {
        if (vehicle == null) return float.MaxValue;
        if (vehicle.useRigidbody == null)
            return (position - vehicle.transform.position).magnitude;
        return (position - vehicle.transform.TransformPoint(vehicle.useRigidbody.centerOfMass)).magnitude;
    }

    public static float GetDistanceTo(this SubRoot subRoot, Vector3 position)
    {
        if (subRoot == null) return float.MaxValue;
        if (subRoot is BaseRoot br) return br.GetDistanceTo(position);
        return (position - subRoot.GetWorldCenterOfMass()).magnitude;
    }

    public static float GetDistanceToPlayer(this BaseRoot baseRoot)
    {
        if (Player.main == null) return float.MaxValue;
        return baseRoot.GetDistanceTo(Player.main.transform.position);
    }

    public static float GetDistanceToPlayer(this Vehicle vehicle)
    {
        if (vehicle == null || Player.main == null) return float.MaxValue;
        return vehicle.GetDistanceTo(Player.main.transform.position);
    }

    public static float GetDistanceToPlayer(this SubRoot subRoot)
    {
        if (subRoot == null || Player.main == null) return float.MaxValue;
        return subRoot.GetDistanceTo(Player.main.transform.position);
    }

    private sealed class SearchOriginScope : IDisposable
    {
        private readonly Transform? _previous;
        private bool _disposed;

        public SearchOriginScope(Transform? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _searchOrigin = _previous;
            _disposed = true;
        }
    }
}
