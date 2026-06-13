namespace InferiusQoL.Features.MobileResourceScanner;

using System.Collections.Generic;
using InferiusQoL.Assets;
using InferiusQoL.Logging;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using UnityEngine;

internal static class MobileResourceScannerItem
{
    public const string ClassId = "InferiusMobileResourceScanner";

    public static TechType TechType { get; private set; } = TechType.None;

    public static void Register()
    {
        var info = PrefabInfo
            .WithTechType(
                ClassId,
                "Mobile Resource Scanner",
                "Equip with a Scanner to track one selected resource around you.")
            .WithIcon(IconLoader.LoadOrFallback("MobileScannerChip.png", TechType.MapRoomHUDChip))
            .WithSizeInInventory(new Vector2int(1, 1));

        var prefab = new CustomPrefab(info);
        prefab.SetGameObject(new CloneTemplate(info, TechType.MapRoomHUDChip));
        prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
        prefab.SetUnlock(TechType.MapRoomHUDChip);
        prefab.SetEquipment(EquipmentType.Chip)
            .WithQuickSlotType(QuickSlotType.Passive);

        prefab.SetRecipe(new RecipeData
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.MapRoomHUDChip, 1),
                    new Ingredient(TechType.Magnetite, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1)
                }
            })
            .WithFabricatorType(CraftTree.Type.MapRoom)
            .WithCraftingTime(5f);

        prefab.Register();
        TechType = info.TechType;
        KnownTechHandler.UnlockOnStart(TechType);

        QoLLog.Info(Category.MobileScanner, $"Registered Mobile Resource Scanner chip as {TechType}");
    }

    public static bool IsEquipped()
    {
        if (TechType == TechType.None)
            return false;

        return Inventory.main?.equipment?.GetCount(TechType) > 0;
    }
}
