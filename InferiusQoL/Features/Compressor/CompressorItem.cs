namespace InferiusQoL.Features.Compressor;

using System.Collections.Generic;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;

/// <summary>
/// Compressor chip: when equipped in the Chip slot, reduces the size of all
/// inventory items to 1x1 except items from CompressorBlacklist. Recipe is in the
/// Modification Station as a mid-late game tier.
/// </summary>
public static class CompressorItem
{
    public const string ClassId = "InferiusCompressor";
    public static TechType TechType { get; private set; } = TechType.None;

    public static void Register()
    {
        var info = PrefabInfo.WithTechType(
            ClassId,
            "Inventory Compressor",
            "Advanced chip that compresses most inventory items to 1x1. Equip in a Chip slot.");

        // Placeholder icon: Scanner, the closest tool look.
        info.WithIcon(InferiusQoL.Assets.IconLoader.LoadOrFallback("Lis.png", TechType.Scanner));
        info.WithSizeInInventory(new Vector2int(1, 1));

        var prefab = new CustomPrefab(info);
        prefab.SetGameObject(new CloneTemplate(info, TechType.Compass));

        var cfg = InferiusConfig.Instance;
        if (cfg.CompressorCraftable)
        {
            // Full craft integration - PDA + craft tree + unlock.
            prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
            prefab.SetUnlock(TechType.AdvancedWiringKit);

            var crafting = prefab.SetRecipe(new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.AdvancedWiringKit, 1),
                    new Ingredient(TechType.Magnetite, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.Polyaniline, 1),
                }
            })
            .WithFabricatorType(CraftTree.Type.Workbench)
            .WithCraftingTime(10f);
            if (Plugin.HasRadialMenu)
                crafting.WithStepsToFabricatorTab("CompressorMenu");
        }
        else
        {
            //QoLLog.Info(Category.Compressor, "Compressor NOT craftable (config). TechType registered, use `spawn InferiusCompressor` in console.");
        }

        prefab.SetEquipment(EquipmentType.Chip)
            .WithQuickSlotType(QuickSlotType.Passive);

        prefab.Register();
        TechType = info.TechType;

        QoLLog.Info(Category.Compressor, $"Registered Compressor chip as {TechType}");
    }

    public static void RegisterTabs()
    {
        // Register the tab only when the chip is craftable; otherwise it is an empty Workbench tab.
        if (!InferiusConfig.Instance.CompressorCraftable) return;
        // Without a radial menu mod, Workbench tabs overlap; skip.
        if (!Plugin.HasRadialMenu) return;

        var label = InferiusQoL.Localization.L.GetOrFallback(
            "InferiusQoL.Tab.Compressor",
            "Inventory Compressor");
        QoLLog.Info(Category.Compressor,
            $"Adding craft tree tab 'CompressorMenu' with label '{label}' to Workbench");
        Nautilus.Handlers.CraftTreeHandler.AddTabNode(
            CraftTree.Type.Workbench,
            "CompressorMenu",
            label,
            SpriteManager.Get(TechType.Scanner));
    }

    /// <summary>Is the chip equipped in the Player equipment Chip slot?</summary>
    public static bool IsEquipped()
    {
        if (TechType == TechType.None) return false;
        var eq = Inventory.main?.equipment;
        if (eq == null) return false;
        return eq.GetCount(TechType) > 0;
    }
}
