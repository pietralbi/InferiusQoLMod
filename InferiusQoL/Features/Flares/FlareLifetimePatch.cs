#nullable disable
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.Flares;

internal static class FlareLifetime
{
    private static readonly TechType FlareTechType = (TechType)754;

    public static void StartOrResume(Flare flare, bool wasUsed)
    {
        if (!IsFlare(flare) || !TryGetTime(out float now))
        {
            return;
        }

        FlareLifetimeData data = GetData(flare, create: true);
        if ((Object)(object)data == (Object)null)
        {
            return;
        }

        bool startFresh = !data.hasStarted || !wasUsed;
        if (startFresh)
        {
            data.hasStarted = true;
            data.startedAt = now;
            data.baseEnergy = Mathf.Max(flare.energyLeft, 0f);
            data.isPaused = false;
            data.pausedAt = 0f;
        }
        else if (data.isPaused)
        {
            data.startedAt += Mathf.Max(0f, now - data.pausedAt);
            data.isPaused = false;
            data.pausedAt = 0f;
        }

        ApplyElapsed(flare, data, syncVisualStart: true);
    }

    public static void Pause(Flare flare)
    {
        if (!IsFlare(flare) || !TryGetTime(out float now))
        {
            return;
        }

        FlareLifetimeData data = GetData(flare, create: true);
        if ((Object)(object)data == (Object)null)
        {
            return;
        }

        if (!data.hasStarted)
        {
            data.hasStarted = true;
            data.startedAt = now;
            data.baseEnergy = Mathf.Max(flare.energyLeft, 0f);
        }
        else if (!data.isPaused)
        {
            ApplyElapsed(flare, data, syncVisualStart: false);
        }

        data.baseEnergy = Mathf.Max(flare.energyLeft, 0f);
        data.startedAt = now;
        data.isPaused = true;
        data.pausedAt = now;
        flare.flareActivateTime = now;
    }

    public static void ApplyElapsedIfBurning(Flare flare)
    {
        if (!IsFlare(flare))
        {
            return;
        }

        if (ShouldPause(flare))
        {
            if (IsUsed(flare))
            {
                Pause(flare);
            }
            return;
        }

        FlareLifetimeData data = GetData(flare, create: false);
        if ((Object)(object)data == (Object)null || !data.hasStarted)
        {
            return;
        }

        if (data.isPaused)
        {
            StartOrResume(flare, wasUsed: true);
            return;
        }

        ApplyElapsed(flare, data, syncVisualStart: true);
    }

    public static void RestoreLoadedWorldFlare(Flare flare)
    {
        if (!IsFlare(flare) || !IsUsed(flare))
        {
            return;
        }

        if (ShouldPause(flare))
        {
            Pause(flare);
            return;
        }

        StartOrResume(flare, wasUsed: true);

        if (flare.energyLeft > 0f)
        {
            flare.hasBeenThrown = true;
            flare.flareActiveState = true;

            if ((Object)(object)flare.light != (Object)null)
            {
                flare.light.enabled = true;
            }

            if ((Object)(object)flare.loopingSound != (Object)null)
            {
                flare.loopingSound.Play();
            }

            if ((Object)(object)flare.fxControl != (Object)null && !flare.fxIsPlaying)
            {
                flare.fxControl.Play(1);
                flare.fxIsPlaying = true;
            }
        }

        if ((Object)(object)flare.capRenderer != (Object)null)
        {
            flare.capRenderer.enabled = false;
        }
    }

    public static bool IsUsed(Flare flare)
    {
        return (Object)(object)flare != (Object)null && (flare.hasBeenThrown || flare.flareActiveState);
    }

    public static bool ShouldPause(Flare flare)
    {
        Pickupable pickupable = GetPickupable(flare);
        if ((Object)(object)pickupable == (Object)null)
        {
            return false;
        }

        if (flare.isDrawn || (Object)(object)((PlayerTool)flare).usingPlayer != (Object)null)
        {
            return false;
        }

        return pickupable.inventoryItem != null || pickupable.attached;
    }

    private static void ApplyElapsed(Flare flare, FlareLifetimeData data, bool syncVisualStart)
    {
        if ((Object)(object)data == (Object)null || !data.hasStarted || data.isPaused || !TryGetTime(out float now))
        {
            return;
        }

        float elapsed = Mathf.Max(0f, now - data.startedAt);
        float expectedRemaining = Mathf.Max(data.baseEnergy - elapsed, 0f);
        flare.energyLeft = Mathf.Min(Mathf.Max(flare.energyLeft, 0f), expectedRemaining);

        if (syncVisualStart)
        {
            flare.flareActivateTime = data.startedAt;
        }
    }

    private static FlareLifetimeData GetData(Flare flare, bool create)
    {
        if ((Object)(object)flare == (Object)null)
        {
            return null;
        }

        GameObject gameObject = ((Component)flare).gameObject;
        FlareLifetimeData data = gameObject.GetComponent<FlareLifetimeData>();
        if ((Object)(object)data == (Object)null && create)
        {
            data = gameObject.AddComponent<FlareLifetimeData>();
        }

        return data;
    }

    private static bool IsFlare(Flare flare)
    {
        Pickupable pickupable = GetPickupable(flare);
        return (Object)(object)pickupable != (Object)null && (int)pickupable.GetTechType() == (int)FlareTechType;
    }

    private static Pickupable GetPickupable(Flare flare)
    {
        if ((Object)(object)flare == (Object)null)
        {
            return null;
        }

        Pickupable pickupable = ((PlayerTool)flare).pickupable;
        if ((Object)(object)pickupable != (Object)null)
        {
            return pickupable;
        }

        return ((Component)flare).GetComponent<Pickupable>();
    }

    private static bool TryGetTime(out float time)
    {
        if ((Object)(object)DayNightCycle.main == (Object)null)
        {
            time = 0f;
            return false;
        }

        time = DayNightCycle.main.timePassedAsFloat;
        return true;
    }
}

[HarmonyPatch(typeof(Flare), nameof(Flare.SetFlareActiveState))]
internal static class Flare_SetFlareActiveState_Lifetime_Patch
{
    private struct State
    {
        public bool WasUsed;
    }

    [HarmonyPrefix]
    private static void Prefix(Flare __instance, bool newFlareActiveState, ref State __state)
    {
        if (newFlareActiveState)
        {
            __state.WasUsed = FlareLifetime.IsUsed(__instance);
        }
    }

    [HarmonyPostfix]
    private static void Postfix(Flare __instance, bool newFlareActiveState, State __state)
    {
        if (newFlareActiveState)
        {
            FlareLifetime.StartOrResume(__instance, __state.WasUsed);
        }
    }
}

[HarmonyPatch(typeof(Flare), nameof(Flare.Update))]
internal static class Flare_Update_Lifetime_Patch
{
    [HarmonyPrefix]
    private static void Prefix(Flare __instance)
    {
        FlareLifetime.ApplyElapsedIfBurning(__instance);
    }
}

[HarmonyPatch(typeof(Flare), nameof(Flare.Awake))]
internal static class Flare_Awake_Lifetime_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Flare __instance)
    {
        FlareLifetime.RestoreLoadedWorldFlare(__instance);
    }
}

[HarmonyPatch(typeof(Flare), nameof(Flare.OnDraw))]
internal static class Flare_OnDraw_Lifetime_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Flare __instance)
    {
        if (FlareLifetime.IsUsed(__instance))
        {
            FlareLifetime.StartOrResume(__instance, wasUsed: true);
        }
    }
}

[HarmonyPatch(typeof(Flare), nameof(Flare.OnHolster))]
internal static class Flare_OnHolster_Lifetime_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Flare __instance)
    {
        if (FlareLifetime.IsUsed(__instance) && FlareLifetime.ShouldPause(__instance))
        {
            FlareLifetime.Pause(__instance);
        }
    }
}

[HarmonyPatch(typeof(Pickupable), nameof(Pickupable.Initialize))]
internal static class Pickupable_Initialize_FlareLifetime_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Pickupable __instance)
    {
        Flare flare = __instance != null ? ((Component)__instance).GetComponent<Flare>() : null;
        if (FlareLifetime.IsUsed(flare))
        {
            FlareLifetime.Pause(flare);
        }
    }
}

[HarmonyPatch(typeof(ItemsContainer), nameof(ItemsContainer.UnsafeAdd))]
internal static class ItemsContainer_UnsafeAdd_FlareLifetime_Patch
{
    [HarmonyPostfix]
    private static void Postfix(InventoryItem item)
    {
        Pickupable pickupable = item != null ? item.item : null;
        Flare flare = (Object)(object)pickupable != (Object)null ? ((Component)pickupable).GetComponent<Flare>() : null;
        if (FlareLifetime.IsUsed(flare) && FlareLifetime.ShouldPause(flare))
        {
            FlareLifetime.Pause(flare);
        }
    }
}
