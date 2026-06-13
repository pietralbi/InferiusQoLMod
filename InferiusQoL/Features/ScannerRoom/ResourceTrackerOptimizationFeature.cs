namespace InferiusQoL.Features.ScannerRoom;

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

internal static class ResourceTrackerOptimizationFeature
{
    private const float VanillaGatherAllRange = 500f;

    internal static void GatherAllNodesInRange(Vector3 position, HashSet<ResourceTrackerDatabase.ResourceInfo> nodes, List<TechType> techTypes)
    {
        nodes.Clear();
        techTypes.Clear();

        var rangeSqr = VanillaGatherAllRange * VanillaGatherAllRange;
        foreach (var resourceSet in ResourceTrackerDatabase.resources)
        {
            var hasNearbyNode = false;
            foreach (var resource in resourceSet.Value.Values)
            {
                if ((position - resource.position).sqrMagnitude > rangeSqr)
                    continue;

                if (!hasNearbyNode)
                {
                    techTypes.Add(resourceSet.Key);
                    hasNearbyNode = true;
                }

                nodes.Add(resource);
            }
        }
    }
}

[HarmonyPatch(typeof(uGUI_ResourceTracker), nameof(uGUI_ResourceTracker.GatherAll))]
internal static class ScannerRoom_ResourceTracker_GatherAll_Patch
{
    public static bool Prefix(HashSet<ResourceTrackerDatabase.ResourceInfo> ___nodes, List<TechType> ___techTypes)
    {
        var camera = MainCamera.camera;
        if (camera == null)
            return true;

        ResourceTrackerOptimizationFeature.GatherAllNodesInRange(camera.transform.position, ___nodes, ___techTypes);
        return false;
    }
}
