namespace InferiusQoL.Features.TankWelder;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using UnityEngine;

public enum MergedTankTier
{
    None = 0,
    T1 = 1,
    T2 = 2,
    T3 = 3,
    T4 = 4
}

/// <summary>
/// 4 merged oxygen tank tiers. Each is cloned from PlasteelTank (Ultra High
/// Capacity Tank), and its capacity is set when the prefab spawns through the
/// CloneTemplate.ModifyPrefab delegate. A Harmony patch on Oxygen.Awake did not
/// run on the cloned prefab.
///
/// Progression:
///   T1 - unlocked with the PlasteelTank blueprint (2x Plasteel + WiringKit)
///   T2 - unlocked after crafting T1        (2x Plasteel + AdvWiring + Magnetite)
///   T3 - unlocked after crafting T2        (2x Plasteel + AdvWiring + Polyaniline + Kyanite)
///   T4 - unlocked after crafting T3, lightweight with no speed penalty
/// </summary>
public static class TankWelderItems
{
    public static TechType MergedTankT1 { get; private set; } = TechType.None;
    public static TechType MergedTankT2 { get; private set; } = TechType.None;
    public static TechType MergedTankT3 { get; private set; } = TechType.None;
    public static TechType MergedTankT4 { get; private set; } = TechType.None;

    public static void RegisterTabs()
    {
        if (!Plugin.HasRadialMenu) return;

        var label = InferiusQoL.Localization.L.GetOrFallback(
            "InferiusQoL.Tab.TankWelder",
            "Merged Tanks");
        QoLLog.Info(Category.TankWelder,
            $"Adding craft tree tab 'TankWelderMenu' with label '{label}' to Workbench");
        Nautilus.Handlers.CraftTreeHandler.AddTabNode(
            CraftTree.Type.Workbench,
            "TankWelderMenu",
            label,
            SpriteManager.Get(TechType.PlasteelTank));
    }

    public static void Register()
    {
        var cfg = InferiusConfig.Instance;

        MergedTankT1 = RegisterMergedTank(
            classId: "InferiusMergedTankT1",
            displayName: "Merged Ultra Tank T1",
            description: "Combined oxygen tank. Capacity = 2x Plasteel x T1 multiplier.",
            unlockAfter: TechType.PlasteelTank,
            multiplier: cfg.TankWelderT1Multiplier,
            lightweight: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.PlasteelTank, 2),
                    new Ingredient(TechType.WiringKit, 1),
                }
            });

        MergedTankT2 = RegisterMergedTank(
            classId: "InferiusMergedTankT2",
            displayName: "Merged Ultra Tank T2",
            description: "Higher-capacity merged tank. Recipe unlocked after crafting T1.",
            unlockAfter: MergedTankT1,
            multiplier: cfg.TankWelderT2Multiplier,
            lightweight: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.PlasteelTank, 2),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.Magnetite, 1),
                }
            });

        MergedTankT3 = RegisterMergedTank(
            classId: "InferiusMergedTankT3",
            displayName: "Merged Ultra Tank T3",
            description: "Advanced merged tank. Recipe unlocked after crafting T2.",
            unlockAfter: MergedTankT2,
            multiplier: cfg.TankWelderT3Multiplier,
            lightweight: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.PlasteelTank, 2),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.Polyaniline, 1),
                    new Ingredient(TechType.Kyanite, 1),
                }
            });

        MergedTankT4 = RegisterMergedTank(
            classId: "InferiusMergedTankT4",
            displayName: "Lightweight Merged Tank T4",
            description: "Advanced lightweight merged tank without swim speed penalty.",
            unlockAfter: MergedTankT3,
            multiplier: cfg.TankWelderT4Multiplier,
            lightweight: true,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(MergedTankT3, 1),
                    new Ingredient(TechType.Aerogel, 2),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(TechType.Polyaniline, 1),
                }
            });

        // T1 is unlocked from the start. The recipe still requires 2x PlasteelTank,
        // so progression is gated by materials, not unlock gates.
        KnownTechHandler.UnlockOnStart(MergedTankT1);

        QoLLog.Info(Category.TankWelder,
            $"Registered Merged Tanks: T1={MergedTankT1}, T2={MergedTankT2}, T3={MergedTankT3}, T4={MergedTankT4}");
    }

    private static TechType RegisterMergedTank(
        string classId,
        string displayName,
        string description,
        TechType unlockAfter,
        float multiplier,
        bool lightweight,
        RecipeData recipe)
    {
        var info = PrefabInfo.WithTechType(classId, displayName, description);
        var iconFile = classId switch
        {
            "InferiusMergedTankT1" => "TankMK1.png",
            "InferiusMergedTankT2" => "TankMK2.png",
            "InferiusMergedTankT3" => "TankMK3.png",
            "InferiusMergedTankT4" => "TankMK4.png",
            _ => null,
        };
        info.WithIcon(iconFile != null
            ? InferiusQoL.Assets.IconLoader.LoadOrFallback(iconFile, TechType.PlasteelTank)
            : SpriteManager.Get(TechType.PlasteelTank));

        // Explicit inventory size (vanilla PlasteelTank = 3x3). Without this,
        // Nautilus defaults to 1x1 for custom TechTypes.
        info.WithSizeInInventory(new Vector2int(3, 3));

        var prefab = new CustomPrefab(info);

        var cloneTemplate = new CloneTemplate(info, TechType.PlasteelTank)
        {
            ModifyPrefab = (obj) => ConfigureMergedTankPrefab(obj, classId, multiplier, lightweight),
        };
        prefab.SetGameObject(cloneTemplate);

        prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
        prefab.SetUnlock(unlockAfter);
        var crafting = prefab.SetRecipe(recipe)
            .WithFabricatorType(CraftTree.Type.Workbench)
            .WithCraftingTime(8f);
        if (Plugin.HasRadialMenu)
            crafting.WithStepsToFabricatorTab("TankWelderMenu");
        prefab.SetEquipment(EquipmentType.Tank)
            .WithQuickSlotType(QuickSlotType.Passive);

        prefab.Register();
        return info.TechType;
    }

    private static void ConfigureMergedTankPrefab(GameObject obj, string classId, float multiplier, bool lightweight)
    {
        var oxygen = obj.GetComponent<Oxygen>();
        if (oxygen == null)
        {
            QoLLog.Error(Category.TankWelder, $"ModifyPrefab {classId}: Oxygen component NOT FOUND on prefab!");
            return;
        }

        var baseCapacity = oxygen.oxygenCapacity; // vanilla PlasteelTank value
        var newCapacity = baseCapacity * 2f * multiplier;
        oxygen.oxygenCapacity = newCapacity;

        QoLLog.Info(Category.TankWelder,
            $"ModifyPrefab {classId}: sourceCapacity={baseCapacity:0.0}s x 2 x {multiplier:0.00} -> {newCapacity:0.0}s (lightweight={lightweight})");
    }
}
