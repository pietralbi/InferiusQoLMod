namespace InferiusQoL.Features.Compressor;

using HarmonyLib;
using InferiusQoL.Config;
using UnityEngine;

/// <summary>
/// Per-instance compression through 4 synchronized patches:
///
/// 1. InventoryItem(Pickupable) constructor postfix: if the instance has a marker,
///    set _width=1, _height=1 (per-instance layout marker).
///
/// 2. ItemsContainer.AddItem prefix/postfix: set ThreadStatic context to the
///    Pickupable. During container layout decisions (free space search, grid
///    placement), TechData.GetItemSize returns 1x1 if the instance is marked.
///
/// 3. uGUI_ItemsContainer.OnAddItem prefix/postfix: set context to the
///    InventoryItem for UI sprite rendering.
///
/// 4. TechData.GetItemSize postfix: read context and return 1x1 if the instance
///    is marked. If there is no context, leave vanilla behavior.
/// </summary>
internal static class CompressorRenderContext
{
    [System.ThreadStatic]
    private static InventoryItem? _currentInventoryItem;

    [System.ThreadStatic]
    private static Pickupable? _currentPickupable;

    public static InventoryItem? CurrentInventoryItem
    {
        get => _currentInventoryItem;
        set => _currentInventoryItem = value;
    }

    public static Pickupable? CurrentPickupable
    {
        get => _currentPickupable;
        set => _currentPickupable = value;
    }

    /// <summary>Returns the current Pickupable in context, primarily through CurrentPickupable with InventoryItem.item as fallback.</summary>
    public static Pickupable? EffectivePickupable =>
        _currentPickupable ?? _currentInventoryItem?.item;
}

[HarmonyPatch(typeof(InventoryItem), MethodType.Constructor, new[] { typeof(Pickupable) })]
public static class InventoryItem_Constructor_Patch
{
    [HarmonyPostfix]
    public static void Postfix(InventoryItem __instance, Pickupable pickupable)
    {
        if (__instance == null || pickupable == null) return;

        var cfg = InferiusConfig.Instance;
        if (!cfg.CompressorEnabled) return;

        var uid = pickupable.GetComponent<UniqueIdentifier>();
        if (uid == null || string.IsNullOrEmpty(uid.Id)) return;
        if (!CompressorSaveManager.IsInstanceCompressed(uid.Id)) return;

        // Self-cleanup: if the TechType is now blacklisted, for example because the
        // user added our custom items later, delete the marker and restore vanilla size.
        var tt = pickupable.GetTechType();
        if (CompressorBlacklist.IsBlacklisted(tt))
        {
            CompressorSaveManager.Remove(uid.Id);
            return;
        }

        __instance._width = 1;
        __instance._height = 1;
    }
}

[HarmonyPatch(typeof(ItemsContainer), nameof(ItemsContainer.AddItem), new[] { typeof(Pickupable) })]
public static class ItemsContainer_AddItem_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Pickupable pickupable)
    {
        // Do not start context if blacklisted; vanilla size is correct.
        if (pickupable != null && CompressorBlacklist.IsBlacklisted(pickupable.GetTechType()))
        {
            CompressorRenderContext.CurrentPickupable = null;
            return;
        }
        CompressorRenderContext.CurrentPickupable = pickupable;
    }

    [HarmonyPostfix]
    public static void Postfix()
    {
        CompressorRenderContext.CurrentPickupable = null;
    }
}

[HarmonyPatch(typeof(uGUI_ItemsContainer), "OnAddItem")]
public static class uGUI_ItemsContainer_OnAddItem_Patch
{
    [HarmonyPrefix]
    public static void Prefix(InventoryItem item)
    {
        CompressorRenderContext.CurrentInventoryItem = item;
    }

    [HarmonyPostfix]
    public static void Postfix()
    {
        CompressorRenderContext.CurrentInventoryItem = null;
    }
}

[HarmonyPatch(typeof(TechData), nameof(TechData.GetItemSize), new[] { typeof(TechType) })]
public static class TechData_GetItemSize_Patch
{
    [HarmonyPostfix]
    public static void Postfix(TechType techType, ref Vector2int __result)
    {
        var cfg = InferiusConfig.Instance;
        if (!cfg.CompressorEnabled) return;
        if (__result.x <= 1 && __result.y <= 1) return;

        var pickupable = CompressorRenderContext.EffectivePickupable;
        if (pickupable == null) return;

        // Sanity: context TechType must match the requested one.
        if (pickupable.GetTechType() != techType) return;

        var uid = pickupable.GetComponent<UniqueIdentifier>();
        if (uid == null || string.IsNullOrEmpty(uid.Id)) return;
        if (!CompressorSaveManager.IsInstanceCompressed(uid.Id)) return;

        __result = new Vector2int(1, 1);
    }
}
