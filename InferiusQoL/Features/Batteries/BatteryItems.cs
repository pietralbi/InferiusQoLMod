namespace InferiusQoL.Features.Batteries;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;

/// <summary>
/// Battery rework - pridava 4 nove TechTypes:
///   Reinforced Battery (tier mezi vanilla Battery a Ion Battery)
///   Reinforced Power Cell
///   Hyper Battery (late-game strop)
///   Hyper Power Cell
///
/// Kapacita se nastavuje pri spawn prefabu pres CloneTemplate.ModifyPrefab
/// (stejny pattern jako merged tanks - Harmony Awake patch na Battery neni
/// spolehlivy pro clone prefab).
///
/// Recept:
///   Reinforced Battery: vanilla Battery + Ruby x2 + Magnetite + WiringKit
///   Reinforced Power Cell: 2x Reinforced Battery + Silicone + WiringKit
///   Hyper Battery: Ion Battery + Magnetite x2 + Kyanite + Aerogel
///   Hyper Power Cell: 2x Hyper Battery + Silicone + AdvancedWiringKit
/// </summary>
public static class BatteryItems
{
    public static TechType ReinforcedBattery { get; private set; } = TechType.None;
    public static TechType ReinforcedPowerCell { get; private set; } = TechType.None;
    public static TechType HyperBattery { get; private set; } = TechType.None;
    public static TechType HyperPowerCell { get; private set; } = TechType.None;

    // Tracking pro EnergyMixinPatch - custom battery/power cell TechTypes
    // ktere je potreba injektovat do compatibleBatteries listu na tools/vehicles.
    public static readonly List<TechType> CustomBatteryTypes = new List<TechType>();
    public static readonly List<TechType> CustomPowerCellTypes = new List<TechType>();

    public static void RegisterTabs()
    {
        if (!Plugin.HasRadialMenu) return;

        var label = InferiusQoL.Localization.L.GetOrFallback(
            "InferiusQoL.Tab.BatteryUpgrades",
            "Battery Upgrades");
        QoLLog.Info(Category.Battery,
            $"Adding craft tree tab 'BatteryUpgradesMenu' with label '{label}' to Workbench");
        Nautilus.Handlers.CraftTreeHandler.AddTabNode(
            CraftTree.Type.Workbench,
            "BatteryUpgradesMenu",
            label,
            SpriteManager.Get(TechType.PrecursorIonBattery));
    }

    public static void Register()
    {
        var cfg = InferiusConfig.Instance;

        // ============================================================
        // Reinforced Battery (mezistupen mezi Battery a Ion Battery)
        // ============================================================
        ReinforcedBattery = RegisterBattery(
            classId: "InferiusReinforcedBattery",
            displayName: "Reinforced Battery",
            description: "Higher-capacity battery.",
            cloneFrom: TechType.Battery,
            unlockAfter: TechType.Battery,
            capacity: cfg.ReinforcedBatteryCapacity,
            isPowerCell: false,
            isHyper: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.Battery, 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(TechType.Magnetite, 1),
                    new Ingredient(TechType.WiringKit, 1),
                }
            });

        ReinforcedPowerCell = RegisterBattery(
            classId: "InferiusReinforcedPowerCell",
            displayName: "Reinforced Power Cell",
            description: "Higher-capacity power cell.",
            cloneFrom: TechType.PowerCell,
            unlockAfter: TechType.PowerCell,
            capacity: cfg.ReinforcedPowerCellCapacity,
            isPowerCell: true,
            isHyper: false,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.PowerCell, 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(TechType.Magnetite, 1),
                    new Ingredient(TechType.WiringKit, 1),
                }
            });

        // ============================================================
        // Hyper Battery/Cell (endgame strop, nad Ion)
        // ============================================================
        HyperBattery = RegisterBattery(
            classId: "InferiusHyperBattery",
            displayName: "Hyper Battery",
            description: "Advanced battery. Highest capacity available.",
            cloneFrom: TechType.PrecursorIonBattery,
            unlockAfter: TechType.PrecursorIonBattery,
            capacity: cfg.HyperBatteryCapacity,
            isPowerCell: false,
            isHyper: true,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.PrecursorIonBattery, 1),
                    new Ingredient(TechType.Magnetite, 2),
                    new Ingredient(TechType.Kyanite, 1),
                    new Ingredient(TechType.Aerogel, 1),
                }
            });

        HyperPowerCell = RegisterBattery(
            classId: "InferiusHyperPowerCell",
            displayName: "Hyper Power Cell",
            description: "Advanced power cell. Highest capacity available.",
            cloneFrom: TechType.PrecursorIonPowerCell,
            unlockAfter: TechType.PrecursorIonPowerCell,
            capacity: cfg.HyperPowerCellCapacity,
            isPowerCell: true,
            isHyper: true,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.PrecursorIonPowerCell, 1),
                    new Ingredient(TechType.Magnetite, 2),
                    new Ingredient(TechType.Kyanite, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                }
            });

        QoLLog.Info(Category.Battery,
            $"Registered Batteries: Reinforced {ReinforcedBattery}/{ReinforcedPowerCell}, Hyper {HyperBattery}/{HyperPowerCell}");

        InjectIntoChargers();
    }

    /// <summary>
    /// Pridava nase TechTypes do statickych HashSetu BatteryCharger.compatibleTech
    /// + PowerCellCharger.compatibleTech. Bez toho nabijecky odmitnou nase
    /// custom baterie/clanky - vanilla check `allowedTech.Contains(tt)`.
    /// </summary>
    private static void InjectIntoChargers()
    {
        foreach (var tt in CustomBatteryTypes)
        {
            if (tt != TechType.None && !BatteryCharger.compatibleTech.Contains(tt))
                BatteryCharger.compatibleTech.Add(tt);
        }
        foreach (var tt in CustomPowerCellTypes)
        {
            if (tt != TechType.None && !PowerCellCharger.compatibleTech.Contains(tt))
                PowerCellCharger.compatibleTech.Add(tt);
        }
        QoLLog.Info(Category.Battery,
            $"Charger compatibility injected: BatteryCharger={BatteryCharger.compatibleTech.Count} types, "
            + $"PowerCellCharger={PowerCellCharger.compatibleTech.Count} types");
    }

    private static TechType RegisterBattery(
        string classId,
        string displayName,
        string description,
        TechType cloneFrom,
        TechType unlockAfter,
        int capacity,
        bool isPowerCell,
        RecipeData recipe,
        bool isHyper)
    {
        var info = PrefabInfo.WithTechType(classId, displayName, description);
        var iconFile = classId switch
        {
            "InferiusReinforcedBattery" => "ReBattery.png",
            "InferiusReinforcedPowerCell" => "RePower_Cell.png",
            "InferiusHyperBattery" => "Hyper_Battery.png",
            "InferiusHyperPowerCell" => "Hyper_Power_Cell.png",
            _ => null,
        };
        info.WithIcon(iconFile != null
            ? InferiusQoL.Assets.IconLoader.LoadOrFallback(iconFile, cloneFrom)
            : SpriteManager.Get(cloneFrom));

        var prefab = new CustomPrefab(info);

        var cloneTemplate = new CloneTemplate(info, cloneFrom)
        {
            ModifyPrefab = (obj) => ConfigureBatteryPrefab(obj, classId, capacity),
        };
        prefab.SetGameObject(cloneTemplate);

        prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.Electronics);
        prefab.SetUnlock(unlockAfter);

        // EXPLICITNI EquipmentType - Nautilus CloneTemplate nezkopíruje TechData
        // entries (equipmentType per TT). Bez toho `Equipment.AddItem` slot
        // type check selze a charger nas item odmitne i kdyz je v allowedTech.
        prefab.SetEquipment(isPowerCell ? EquipmentType.PowerCellCharger : EquipmentType.BatteryCharger);

        var crafting = prefab.SetRecipe(recipe).WithCraftingTime(5f);

        if (isHyper)
        {
            // Hyper tier = endgame v Modification Station. S radial menu modem
            // do BatteryUpgradesMenu tabu, jinak primo do rootu Workbenchu.
            var hyperCrafting = crafting.WithFabricatorType(CraftTree.Type.Workbench);
            if (Plugin.HasRadialMenu)
                hyperCrafting.WithStepsToFabricatorTab("BatteryUpgradesMenu");
        }
        else
        {
            // Reinforced tier = standardni Fabricator, Resources/Electronics
            // (vedle vanilla Battery/PowerCell). Vanilla taby - vzdy ok.
            crafting.WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab("Resources", "Electronics");
        }

        prefab.Register();

        // Track pro EnergyMixinPatch - batterie do BatteryTypes, power cells do PowerCellTypes.
        if (isPowerCell)
            CustomPowerCellTypes.Add(info.TechType);
        else
            CustomBatteryTypes.Add(info.TechType);

        // Registrace 3D model source (clone vanilla model + swap texture)
        if (iconFile != null)
            EnergyMixin_Awake_Patch.RegisterModelSource(info.TechType, cloneFrom, iconFile);

        return info.TechType;
    }

    private static void ConfigureBatteryPrefab(GameObject obj, string classId, int newCapacity)
    {
        var battery = obj.GetComponent<Battery>();
        if (battery == null)
        {
            QoLLog.Error(Category.Battery, $"ModifyPrefab {classId}: Battery component NOT FOUND!");
            return;
        }

        var oldCapacity = battery._capacity;
        battery._capacity = newCapacity;
        battery._charge = newCapacity; // fully charged on craft
        QoLLog.Info(Category.Battery,
            $"ModifyPrefab {classId}: capacity {oldCapacity:0.0} -> {newCapacity}");
    }
}
