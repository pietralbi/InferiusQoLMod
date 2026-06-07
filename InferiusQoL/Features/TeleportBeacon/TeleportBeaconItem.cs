namespace InferiusQoL.Features.TeleportBeacon;

using System.Collections.Generic;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using UnityEngine;

/// <summary>
/// Teleport Beacon: buildable interior piece cloned from vanilla StarshipSouvenir
/// (Aurora minimodel). Buildable with the Habitat Builder and stationary after
/// placement. Clicking the beacon starts teleport to the nearest other beacon.
/// </summary>
public static class TeleportBeaconItem
{
    public const string ClassId = "InferiusTeleportBeacon";
    public static TechType TechType { get; private set; } = TechType.None;

    public static void Register()
    {
        var info = PrefabInfo.WithTechType(
            ClassId,
            "Teleport Beacon",
            "Stationary teleport device. Build 2+ across your bases to teleport between them.");

        info.WithIcon(InferiusQoL.Assets.IconLoader.LoadOrFallback("Teleport.png", TechType.Beacon));

        var prefab = new CustomPrefab(info);

        // StarshipSouvenir is not buildable in this Subnautica version; it is only
        // pickupable decoration. Clone Bench instead, a reliable buildable interior
        // piece. A custom Aurora mini model would require an asset bundle and was
        // deferred for later.
        var cloneTemplate = new CloneTemplate(info, TechType.Bench)
        {
            ModifyPrefab = (obj) =>
            {
                // Remove the vanilla Bench component (sit interaction) so our
                // TeleportBeaconBehavior can take over the IHandTarget hook.
                var bench = obj.GetComponent<Bench>();
                if (bench != null)
                    Object.DestroyImmediate(bench);

                // Remove sit-related objects (trigger collider for sitting, sit points).
                // If a "SitPoint" child exists, remove it.
                var sitPoint = obj.transform.Find("SitPoint");
                if (sitPoint != null)
                    Object.DestroyImmediate(sitPoint.gameObject);

                if (obj.GetComponent<TeleportBeaconBehavior>() == null)
                    obj.AddComponent<TeleportBeaconBehavior>();

                // Override TechTag + Constructable techType to our custom type; the clone inherited vanilla.
                var techTag = obj.GetComponent<TechTag>();
                if (techTag != null)
                    techTag.type = info.TechType;

                var constructable = obj.GetComponent<Constructable>();
                if (constructable != null)
                {
                    constructable.techType = info.TechType;
                    constructable.allowedOnGround = true;
                    constructable.allowedInBase = true;
                    constructable.allowedInSub = true;
                    constructable.allowedOutside = false;
                    constructable.allowedOnConstructables = true;
                    constructable.allowedOnWall = false;
                    constructable.allowedOnCeiling = false;
                    constructable.rotationEnabled = true;
                    constructable.deconstructionAllowed = true;
                    constructable.forceUpright = true;
                }

                QoLLog.Info(Category.Teleport,
                    $"ModifyPrefab TeleportBeacon: Constructable {(constructable != null ? "set" : "MISSING")}, TechTag {(techTag != null ? "set" : "MISSING")}");
            },
        };
        prefab.SetGameObject(cloneTemplate);

        prefab.SetPdaGroupCategory(TechGroup.InteriorPieces, TechCategory.InteriorPiece);

        // Habitat Builder buildable: DO NOT USE WithFabricatorType.
        // CraftTree.Type.Constructor = Mobile Vehicle Bay, which was an earlier
        // mistake. Interior pieces are added to the Habitat Builder automatically
        // through TechGroup.InteriorPieces.
        prefab.SetRecipe(new RecipeData
        {
            craftAmount = 1,
            Ingredients = new List<Ingredient>
            {
                new Ingredient(TechType.Titanium, 2),
                new Ingredient(TechType.AdvancedWiringKit, 1),
                new Ingredient(TechType.Kyanite, 1),
                new Ingredient(TechType.Polyaniline, 1),
                new Ingredient(TechType.Aerogel, 1),
            }
        });

        prefab.Register();
        TechType = info.TechType;

        // Unlocked from the start, without Analysis.
        KnownTechHandler.UnlockOnStart(TechType);

        QoLLog.Info(Category.Teleport, $"Registered Teleport Beacon as {TechType} (unlocked on start)");
    }
}
