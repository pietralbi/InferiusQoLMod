namespace InferiusQoL.Features.SeamothTurbo;

using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// Turbo boost for Seamoth (3 tiers: MK1/MK2/MK3). Postfix patch on SeaMoth.Update.
///
/// - Velocity cap: added force only up to target speed, never above it.
/// - Surface falloff: boost smoothly fades as the Seamoth approaches the surface
///   (0% above the surface, 100% at SurfaceFalloffMeters and deeper). This cleanly
///   solves the problem of jumping out of the water.
/// - Vehicle Power Upgrade Module discount: consumption is reduced if the vanilla
///   consumption-reduction module is equipped (0.5^count).
/// - Auto-detects the highest tier (MK3 > MK2 > MK1); tiers do not stack.
/// </summary>
[HarmonyPatch(typeof(SeaMoth), nameof(SeaMoth.Update))]
public static class SeaMoth_Update_Patch
{
    private const float SeamothBaseDrainPerSecond = 0.15f;

    private static TurboTier _loggedTier = TurboTier.None;

    [HarmonyPostfix]
    public static void Postfix(SeaMoth __instance)
    {
        var tier = ShouldBoost(__instance);
        if (tier == TurboTier.None) return;
        ApplyBoost(__instance, tier);
    }

    private static TurboTier ShouldBoost(SeaMoth seamoth)
    {
        if (seamoth == null) return TurboTier.None;

        var cfg = InferiusConfig.Instance;
        if (!cfg.SeamothTurboEnabled) return TurboTier.None;

        var player = Player.main;
        if (player == null) return TurboTier.None;
        if (player.GetVehicle() != seamoth) return TurboTier.None;

        var tier = SeamothTurboItems.GetEquippedTier(seamoth);
        if (tier == TurboTier.None) return TurboTier.None;

        if (!GameInput.GetButtonHeld(GameInput.Button.Sprint)) return TurboTier.None;

        if (!seamoth.HasEnoughEnergy(0.01f)) return TurboTier.None;

        return tier;
    }

    private static void ApplyBoost(SeaMoth seamoth, TurboTier tier)
    {
        var cfg = InferiusConfig.Instance;
        var (speedMult, energyMult) = SeamothTurboItems.GetTierValues(tier, cfg);

        // Surface falloff: smooth reduction of boost near the surface.
        float falloff = GetDepthFalloff(seamoth, cfg.SeamothTurboSurfaceFalloffMeters);
        if (falloff <= 0f) return; // above the surface, no boost

        var speedBonus = (speedMult - 1f) * falloff;
        var energyBonus = (energyMult - 1f) * falloff;

        var rb = seamoth.useRigidbody;
        if (rb != null && speedBonus > 0f)
        {
            Vector3 forward = seamoth.transform.forward;
            float currentForwardSpeed = Vector3.Dot(rb.velocity, forward);
            float targetMaxSpeed = seamoth.forwardForce * (1f + speedBonus);

            if (currentForwardSpeed < targetMaxSpeed)
            {
                float deltaNeeded = targetMaxSpeed - currentForwardSpeed;
                float dt = Time.deltaTime;
                float maxStep = seamoth.forwardForce * speedBonus * dt;
                float stepForce = Mathf.Min(maxStep, deltaNeeded);

                rb.velocity += forward * stepForce;

                if (_loggedTier != tier)
                {
                    _loggedTier = tier;
                    QoLLog.Info(Category.Seamoth,
                        $"Turbo {tier} active: base {speedMult:0.0}x/{energyMult:0.0}x, falloff {falloff:P0}, target {targetMaxSpeed:0.0} m/s");
                }
            }
        }

        // Extra energy drain with PowerUpgradeModule discount.
        if (energyBonus > 0f)
        {
            float powerMult = GetPowerUpgradeMultiplier(seamoth);
            var extra = energyBonus * SeamothBaseDrainPerSecond * Time.deltaTime * powerMult;
            seamoth.energyInterface?.ConsumeEnergy(extra);
        }
    }

    /// <summary>
    /// Returns 0..1 based on depth below the surface.
    /// 0 = at or above the surface (no boost).
    /// 1 = deep below the surface (full boost).
    /// Smooth transition over falloffDistance.
    /// </summary>
    private static float GetDepthFalloff(SeaMoth seamoth, float falloffDistance)
    {
        if (falloffDistance <= 0.01f) return 1f; // falloff disabled
        float oceanLevel = Ocean.GetOceanLevel();
        float depth = oceanLevel - seamoth.transform.position.y;
        return Mathf.Clamp01(depth / falloffDistance);
    }

    /// <summary>
    /// Applies the consumption discount if the vanilla Vehicle Power Upgrade Module is equipped.
    /// Each equipped module halves consumption, matching vanilla behavior.
    /// </summary>
    private static float GetPowerUpgradeMultiplier(SeaMoth seamoth)
    {
        if (seamoth.modules == null) return 1f;
        int count = seamoth.modules.GetCount(TechType.VehiclePowerUpgradeModule);
        if (count <= 0) return 1f;
        return Mathf.Pow(0.5f, count);
    }
}
