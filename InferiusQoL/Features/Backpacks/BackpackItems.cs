namespace InferiusQoL.Features.Backpacks;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;

public enum BackpackTier
{
    None = 0,
    Small = 1,
    Medium = 2,
    Large = 3
}

/// <summary>
/// 3 backpack tiers (Small/Medium/Large). A backpack is a 2x2 inventory item.
/// When present, InventoryResizePatch applies the extra-row bonus. Detects the
/// highest tier and does not stack.
/// </summary>
public static class BackpackItems
{
    public static TechType Small { get; private set; } = TechType.None;
    public static TechType Medium { get; private set; } = TechType.None;
    public static TechType Large { get; private set; } = TechType.None;

    public static void Register()
    {
        // Without a radial menu mod, Workbench tabs overlap. Place upgrades in
        // the root (empty tabPath = root). Small backpack is always in the
        // Fabricator Personal/Equipment tab, which exists in vanilla.
        var workbenchTab = Plugin.HasRadialMenu ? new[] { "BackpackMenu" } : new string[0];

        Small = RegisterTier(
            classId: "InferiusBackpackSmall",
            displayName: "Small Backpack",
            description: "A compact backpack that expands your inventory by a few extra rows.",
            iconReference: TechType.LuggageBag,
            unlockAfter: TechType.None, // default unlocked
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Silicone, 1),
                }
            },
            fabricator: CraftTree.Type.Fabricator,
            tabPath: new[] { "Personal", "Equipment" });

        Medium = RegisterTier(
            classId: "InferiusBackpackMedium",
            displayName: "Medium Backpack",
            description: "Upgraded backpack with more inventory slots. Consumes a Small Backpack during crafting.",
            iconReference: TechType.SmallStorage,
            unlockAfter: Small,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(Small, 1),
                    new Ingredient(TechType.Titanium, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.Lithium, 1),
                }
            },
            fabricator: CraftTree.Type.Workbench,
            tabPath: workbenchTab);

        Large = RegisterTier(
            classId: "InferiusBackpackLarge",
            displayName: "Large Backpack",
            description: "Advanced backpack with the most inventory slots. Consumes a Medium Backpack during crafting.",
            iconReference: TechType.VehicleStorageModule,
            unlockAfter: Medium,
            recipe: new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(Medium, 1),
                    new Ingredient(TechType.Kyanite, 1),
                    new Ingredient(TechType.Polyaniline, 1),
                    new Ingredient(TechType.EnameledGlass, 1),
                }
            },
            fabricator: CraftTree.Type.Workbench,
            tabPath: workbenchTab);

        QoLLog.Info(Category.Backpack,
            $"Registered backpacks: Small={Small}, Medium={Medium}, Large={Large}");
    }

    /// <summary>
    /// Must add a custom tab to the Modification Station for Medium/Large backpacks.
    /// Call before Register().
    /// </summary>
    public static void RegisterTabs()
    {
        // Without a radial menu mod, Workbench tabs overlap. Skip tab creation;
        // upgrades go to the Workbench root.
        if (!Plugin.HasRadialMenu) return;

        var label = InferiusQoL.Localization.L.GetOrFallback(
            "InferiusQoL.Tab.BackpackUpgrades",
            "Backpack Upgrades");
        Nautilus.Handlers.CraftTreeHandler.AddTabNode(
            CraftTree.Type.Workbench,
            "BackpackMenu",
            label,
            SpriteManager.Get(TechType.LuggageBag));
    }

    private static TechType RegisterTier(
        string classId,
        string displayName,
        string description,
        TechType iconReference,
        TechType unlockAfter,
        RecipeData recipe,
        CraftTree.Type fabricator,
        string[] tabPath)
    {
        var info = PrefabInfo.WithTechType(classId, displayName, description);
        // Small backpack intentionally uses the vanilla LuggageBag sprite, with no custom PNG.
        var iconFile = classId switch
        {
            "InferiusBackpackSmall" => "BackSmall.png",
            "InferiusBackpackMedium" => "BackMedium.png",
            "InferiusBackpackLarge" => "BackLarge.png",
            _ => null,
        };
        info.WithIcon(iconFile != null
            ? InferiusQoL.Assets.IconLoader.LoadOrFallback(iconFile, iconReference)
            : SpriteManager.Get(iconReference));
        info.WithSizeInInventory(new Vector2int(2, 2));

        var prefab = new CustomPrefab(info);
        prefab.SetGameObject(new CloneTemplate(info, TechType.LuggageBag));
        prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
        if (unlockAfter != TechType.None)
            prefab.SetUnlock(unlockAfter);
        else
            prefab.SetUnlock(TechType.Scanner); // Scanner is default-unlocked, so the item is available from the start.

        var crafting = prefab.SetRecipe(recipe)
            .WithFabricatorType(fabricator)
            .WithCraftingTime(3f);
        if (tabPath.Length > 0)
            crafting.WithStepsToFabricatorTab(tabPath);

        // Chip slot = vanilla Player has one Chip slot, usually Compass.
        // If SlotExtender is installed, it adds more Chip slots, so the player
        // does not have to sacrifice Compass.
        prefab.SetEquipment(EquipmentType.Chip)
            .WithQuickSlotType(QuickSlotType.Passive);

        prefab.Register();
        return info.TechType;
    }

    /// <summary>
    /// Highest backpack tier equipped in the Player equipment Chip slot
    /// (priority Large > Medium > Small).
    /// </summary>
    public static BackpackTier GetEquippedTier()
    {
        var eq = Inventory.main?.equipment;
        if (eq == null) return BackpackTier.None;
        if (Large != TechType.None && eq.GetCount(Large) > 0) return BackpackTier.Large;
        if (Medium != TechType.None && eq.GetCount(Medium) > 0) return BackpackTier.Medium;
        if (Small != TechType.None && eq.GetCount(Small) > 0) return BackpackTier.Small;
        return BackpackTier.None;
    }

    /// <summary>Number of extra rows for the given tier from the current config.</summary>
    public static int GetTierRows(BackpackTier tier, InferiusConfig cfg)
    {
        return tier switch
        {
            BackpackTier.Small => cfg.BackpackSmallRows,
            BackpackTier.Medium => cfg.BackpackMediumRows,
            BackpackTier.Large => cfg.BackpackLargeRows,
            _ => 0
        };
    }
}
