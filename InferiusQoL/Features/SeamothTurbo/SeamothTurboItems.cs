namespace InferiusQoL.Features.SeamothTurbo;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;

public enum TurboTier
{
    None = 0,
    MK1 = 1,
    MK2 = 2,
    MK3 = 3
}

/// <summary>
/// Registrace vsech tri tieru Seamoth Turbo modulu (MK1, MK2, MK3).
/// Kazdy tier ma vlastni recipe s upgrade path: MK2 vyzaduje 1x MK1, MK3 vyzaduje 1x MK2.
/// Kazdy tier ma v configu vlastni slidery pro rychlost a spotrebu.
/// Ikony jsou zatim placeholdery z vanilla Seamoth modulu, nahradime je pozdeji.
/// </summary>
public static class SeamothTurboItems
{
    public static TechType MK1 { get; private set; } = TechType.None;
    public static TechType MK2 { get; private set; } = TechType.None;
    public static TechType MK3 { get; private set; } = TechType.None;

    public static void Register()
    {
        // Vlastni tab v Modification Station (Workbench) pro MK2/MK3 - jen
        // pokud je radial menu mod, jinak Workbench taby prekryvaji.
        if (Plugin.HasRadialMenu)
        {
            var label = InferiusQoL.Localization.L.GetOrFallback(
                "InferiusQoL.Tab.SeamothTurboUpgrades",
                "Seamoth Turbo Upgrades");
            Nautilus.Handlers.CraftTreeHandler.AddTabNode(
                CraftTree.Type.Workbench,
                "SeamothTurboMenu",
                label,
                SpriteManager.Get(TechType.SeamothElectricalDefense));
        }

        // MK1 - ve Vehicle Upgrade Console (Moonpool), vedle vanilla Seamoth modulu.
        MK1 = RegisterTier(
            classId: "InferiusSeamothTurboMK1",
            displayName: "Seamoth Turbo Module MK1",
            description: "Basic speed boost while sprinting.",
            iconReference: TechType.SeamothSolarCharge,
            unlockAfter: TechType.Seamoth,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Copper, 1),
                    new Ingredient(TechType.Magnetite, 2),
                    new Ingredient(TechType.Lubricant, 1),
                }
            },
            fabricator: CraftTree.Type.SeamothUpgrades,
            fabricatorTab: "SeamothModules");

        // MK2 - ve Modification Station, vlastni tab.
        MK2 = RegisterTier(
            classId: "InferiusSeamothTurboMK2",
            displayName: "Seamoth Turbo Module MK2",
            description: "Improved turbo with higher speed cap. Consumes a MK1 module during crafting.",
            iconReference: TechType.SeamothElectricalDefense,
            unlockAfter: MK1,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(MK1, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.Kyanite, 1),
                    new Ingredient(TechType.Aerogel, 1),
                }
            },
            fabricator: CraftTree.Type.Workbench,
            fabricatorTab: "SeamothTurboMenu");

        // MK3 - ve Modification Station, vlastni tab.
        MK3 = RegisterTier(
            classId: "InferiusSeamothTurboMK3",
            displayName: "Seamoth Turbo Module MK3",
            description: "Advanced turbo with maximum boost. Consumes a MK2 module during crafting.",
            iconReference: TechType.SeamothSonarModule,
            unlockAfter: MK2,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(MK2, 1),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(TechType.Polyaniline, 1),
                    new Ingredient(TechType.EnameledGlass, 1),
                }
            },
            fabricator: CraftTree.Type.Workbench,
            fabricatorTab: "SeamothTurboMenu");

        QoLLog.Info(Category.Seamoth,
            $"Registered Seamoth Turbo tiers: MK1={MK1}, MK2={MK2}, MK3={MK3}");
    }

    private static TechType RegisterTier(
        string classId,
        string displayName,
        string description,
        TechType iconReference,
        TechType unlockAfter,
        RecipeData recipe,
        CraftTree.Type fabricator,
        string fabricatorTab)
    {
        var info = PrefabInfo.WithTechType(classId, displayName, description);
        var iconFile = classId switch
        {
            "InferiusSeamothTurboMK1" => "Turbo.png",
            "InferiusSeamothTurboMK2" => "TurboMK2.png",
            "InferiusSeamothTurboMK3" => "TurboMK3.png",
            _ => null,
        };
        info.WithIcon(iconFile != null
            ? InferiusQoL.Assets.IconLoader.LoadOrFallback(iconFile, iconReference)
            : SpriteManager.Get(iconReference));

        var prefab = new CustomPrefab(info);
        prefab.SetGameObject(new CloneTemplate(info, TechType.SeamothSolarCharge));
        prefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);
        prefab.SetUnlock(unlockAfter);
        var crafting = prefab.SetRecipe(recipe)
            .WithFabricatorType(fabricator)
            .WithCraftingTime(5f);
        // SeamothTurboMenu je nas custom Workbench tab - pouzijeme jen kdyz radial menu.
        // Vehicle Upgrade Console (MK1) ma vanilla taby, ty pouzivame vzdy.
        if (fabricator != CraftTree.Type.Workbench || Plugin.HasRadialMenu)
            crafting.WithStepsToFabricatorTab(fabricatorTab);
        prefab.SetEquipment(EquipmentType.VehicleModule)
            .WithQuickSlotType(QuickSlotType.Passive);

        prefab.Register();
        return info.TechType;
    }

    /// <summary>Vrati nejvyssi osazeny tier v danem Seamoth (priorita MK3 > MK2 > MK1).</summary>
    public static TurboTier GetEquippedTier(SeaMoth seamoth)
    {
        if (seamoth == null || seamoth.modules == null) return TurboTier.None;
        if (MK3 != TechType.None && seamoth.modules.GetCount(MK3) > 0) return TurboTier.MK3;
        if (MK2 != TechType.None && seamoth.modules.GetCount(MK2) > 0) return TurboTier.MK2;
        if (MK1 != TechType.None && seamoth.modules.GetCount(MK1) > 0) return TurboTier.MK1;
        return TurboTier.None;
    }

    /// <summary>Rychlostni a energeticky nasobitel pro dany tier (z aktualniho configu).</summary>
    public static (float speedMult, float energyMult) GetTierValues(TurboTier tier, InferiusConfig cfg)
    {
        return tier switch
        {
            TurboTier.MK1 => (cfg.SeamothTurboMK1SpeedMultiplier, cfg.SeamothTurboMK1EnergyMultiplier),
            TurboTier.MK2 => (cfg.SeamothTurboMK2SpeedMultiplier, cfg.SeamothTurboMK2EnergyMultiplier),
            TurboTier.MK3 => (cfg.SeamothTurboMK3SpeedMultiplier, cfg.SeamothTurboMK3EnergyMultiplier),
            _ => (1f, 1f)
        };
    }
}
