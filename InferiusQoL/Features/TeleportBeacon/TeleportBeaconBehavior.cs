namespace InferiusQoL.Features.TeleportBeacon;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// MonoBehaviour on each spawned Teleport Beacon. On interaction, opens the IMGUI
/// UI (TeleportBeaconUI) with a destination list and naming input.
///
/// Per-beacon data (name, efficiency tier) persists in TeleportBeaconSaveManager,
/// stored as JSON beside the DLL with UniqueIdentifier.Id as the key.
/// </summary>
public class TeleportBeaconBehavior : MonoBehaviour, IHandTarget
{
    public static readonly List<TeleportBeaconBehavior> All = new List<TeleportBeaconBehavior>();

    public float cooldownUntil = 0f;

    private TeleportBeaconUI? _ui;

    public string Id => GetComponent<UniqueIdentifier>()?.Id ?? "";

    public TeleportBeaconSaveManager.BeaconData Data =>
        TeleportBeaconSaveManager.GetOrCreate(Id);

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        if (_ui == null) _ui = gameObject.AddComponent<TeleportBeaconUI>();
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void OnHandHover(GUIHand hand)
    {
        if (!InferiusConfig.Instance.TeleportBeaconEnabled) return;

        HandReticle.main.SetIcon(HandReticle.IconType.Interact);
        HandReticle.main.SetText(HandReticle.TextType.Hand, GetHoverText(), false);
    }

    public void OnHandClick(GUIHand hand)
    {
        if (!InferiusConfig.Instance.TeleportBeaconEnabled) return;
        _ui?.Show();
    }

    public string GetHoverText()
    {
        var name = Data.name;
        return All.Count > 1 ? $"Open {name}" : $"{name} (no other beacons)";
    }

    public bool IsOnCooldown(out int secondsRemaining)
    {
        if (Time.time < cooldownUntil)
        {
            secondsRemaining = (int)(cooldownUntil - Time.time);
            return true;
        }
        secondsRemaining = 0;
        return false;
    }

    public bool TryTeleportTo(TeleportBeaconBehavior target, out string failReason)
    {
        failReason = "";
        var cfg = InferiusConfig.Instance;

        if (IsOnCooldown(out int cd))
        {
            failReason = $"Cooldown {cd}s";
            return false;
        }

        var thisRelay = GetComponentInParent<PowerRelay>();
        var targetRelay = target.GetComponentInParent<PowerRelay>();

        // Free teleport: Creative/Freedom mode (vanilla GameModeUtils.RequiresPower
        // = false) OR user-toggle TeleportAlwaysFree. Skip power check + drain.
        bool freeMode = cfg.TeleportAlwaysFree || !GameModeUtils.RequiresPower();

        if (!freeMode)
        {
            if (!HasEnoughPower(thisRelay, cfg.TeleportMinBasePowerPercent))
            {
                failReason = "Source base power too low";
                return false;
            }
            if (!HasEnoughPower(targetRelay, cfg.TeleportMinBasePowerPercent))
            {
                failReason = "Target base power too low";
                return false;
            }
        }

        var distance = Vector3.Distance(transform.position, target.transform.position);
        var distanceCost = (distance / 100f) * cfg.TeleportCostPerHundredMeters;
        var baseCost = cfg.TeleportSourceCostJoules + cfg.TeleportTargetCostJoules + distanceCost;

        // Apply the efficiency chip multiplier, using the highest tier from either beacon.
        var maxTier = System.Math.Max(Data.efficiencyTier, target.Data.efficiencyTier);
        var efficiencyMult = cfg.GetEfficiencyMultiplier(maxTier);
        baseCost *= efficiencyMult;

        var totalSourceCost = baseCost * 0.5f;
        var totalTargetCost = baseCost * 0.5f;

        if (!freeMode)
        {
            DrainPower(thisRelay, totalSourceCost);
            DrainPower(targetRelay, totalTargetCost);
        }

        // Teleport destination = a little in front of the target beacon, so the
        // player lands at a consistent spot beside the beacon instead of inside/on it.
        var targetT = target.transform;
        var destination = targetT.position + targetT.forward * 2f + Vector3.up * 1f;

        var player = Player.main;
        if (player != null)
        {
            player.SetPosition(destination);

            // Look back toward the beacon
            player.transform.rotation = Quaternion.LookRotation(-targetT.forward);

            // Detect target sub so Subnautica knows the player is not in swim mode.
            var targetSub = FindSubAt(destination) ?? target.GetComponentInParent<SubRoot>();
            player.currentSub = targetSub;

            if (player.rigidBody != null)
            {
                player.rigidBody.velocity = Vector3.zero;
                player.rigidBody.ResetInertiaTensor();
            }
        }

        cooldownUntil = Time.time + cfg.TeleportCooldownSeconds;
        target.cooldownUntil = Time.time + cfg.TeleportCooldownSeconds;

        QoLLog.Info(Category.Teleport,
            $"Teleport '{Data.name}' -> '{target.Data.name}' ({distance:0}m, {totalSourceCost + totalTargetCost:0} J total)");
        return true;
    }

    public float EstimateCost(TeleportBeaconBehavior target)
    {
        var cfg = InferiusConfig.Instance;
        if (cfg.TeleportAlwaysFree || !GameModeUtils.RequiresPower()) return 0f;

        var distance = Vector3.Distance(transform.position, target.transform.position);
        var distanceCost = (distance / 100f) * cfg.TeleportCostPerHundredMeters;
        var base_ = cfg.TeleportSourceCostJoules + cfg.TeleportTargetCostJoules + distanceCost;

        var maxTier = System.Math.Max(Data.efficiencyTier, target.Data.efficiencyTier);
        return base_ * cfg.GetEfficiencyMultiplier(maxTier);
    }

    private static SubRoot? FindSubAt(Vector3 position)
    {
        var colliders = Physics.OverlapSphere(position, 2f);
        foreach (var c in colliders)
        {
            if (c == null) continue;
            var sub = c.GetComponentInParent<SubRoot>();
            if (sub != null) return sub;
        }
        return null;
    }

    public static bool HasEnoughPower(PowerRelay? relay, float minPercent)
    {
        if (relay == null) return true;
        if (relay.GetMaxPower() <= 0f) return false;
        var ratio = relay.GetPower() / relay.GetMaxPower() * 100f;
        return ratio >= minPercent;
    }

    public static void DrainPower(PowerRelay? relay, float amount)
    {
        if (relay == null || amount <= 0f) return;
        relay.ConsumeEnergy(amount, out _);
    }
}
