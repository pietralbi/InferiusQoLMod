namespace InferiusQoL.Features.TankWelder;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
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
///   T1 - unlocked with the PlasteelTank blueprint (2x Plasteel Tank + WiringKit)
///   T2 - unlocked after T1                 (T1 + AdvWiring + Magnetite)
///   T3 - unlocked after T2                 (T2 + Polyaniline + Kyanite)
///   T4 - unlocked after T3, lightweight with no speed penalty
/// </summary>
public static class TankWelderItems
{
    private const string CraftTreeTab = "TankWelderMenu";
    private const float CraftingTimeSeconds = 8f;

    private const string T1ClassId = "InferiusMergedTankT1";
    private const string T2ClassId = "InferiusMergedTankT2";
    private const string T3ClassId = "InferiusMergedTankT3";
    private const string T4ClassId = "InferiusMergedTankT4";

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
            $"Adding craft tree tab '{CraftTreeTab}' with label '{label}' to Workbench");
        Nautilus.Handlers.CraftTreeHandler.AddTabNode(
            CraftTree.Type.Workbench,
            CraftTreeTab,
            label,
            SpriteManager.Get(TechType.PlasteelTank));
    }

    public static void Register()
    {
        var cfg = InferiusConfig.Instance;

        MergedTankT1 = RegisterMergedTank(
            classId: T1ClassId,
            displayName: "Merged Ultra Tank T1",
            description: "Combined oxygen tank made from two Plasteel Tanks.",
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
            classId: T2ClassId,
            displayName: "Merged Ultra Tank T2",
            description: "Higher-capacity merged tank. Consumes a T1 tank during crafting.",
            unlockAfter: MergedTankT1,
            multiplier: cfg.TankWelderT2Multiplier,
            lightweight: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(MergedTankT1, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.Magnetite, 1),
                }
            });

        MergedTankT3 = RegisterMergedTank(
            classId: T3ClassId,
            displayName: "Merged Ultra Tank T3",
            description: "Advanced merged tank. Consumes a T2 tank during crafting.",
            unlockAfter: MergedTankT2,
            multiplier: cfg.TankWelderT3Multiplier,
            lightweight: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(MergedTankT2, 1),
                    new Ingredient(TechType.Polyaniline, 1),
                    new Ingredient(TechType.Kyanite, 1),
                }
            });

        MergedTankT4 = RegisterMergedTank(
            classId: T4ClassId,
            displayName: "Lightweight Merged Tank T4",
            description: "Lightweight advanced merged tank without swim speed penalty.",
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
                    new Ingredient(TechType.Kyanite, 1),
                    new Ingredient(TechType.Polyaniline, 1),
                }
            });

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
            T1ClassId => "TankMK1.png",
            T2ClassId => "TankMK2.png",
            T3ClassId => "TankMK3.png",
            T4ClassId => "TankMK4.png",
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
            .WithCraftingTime(CraftingTimeSeconds);
        if (Plugin.HasRadialMenu)
            crafting.WithStepsToFabricatorTab(CraftTreeTab);
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
