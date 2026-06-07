namespace InferiusQoL.Features.AutoCraft;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using InferiusQoL.Logging;
using UnityEngine;

/// <summary>
/// AutoCraft main logic. EasyCraft.Main port with minimal changes:
/// - namespace and references to AutoCraftSettings instead of Main.Settings
/// - logs through QoLLog
/// - direct field access instead of reflection where the publicized API allows it
///
/// Three main entry points:
/// 1. <see cref="GhostCraft"/> - called from the GhostCrafter.Craft prefix
/// 2. <see cref="ConstructorCraft"/> - called from the ConstructorInput.Craft prefix
/// 3. <see cref="Construct"/> - called from the Constructable.Construct prefix
///    (auto-pulls resources from nearby containers while building)
/// </summary>
public static class AutoCraftMain
{
    public const float DefaultRecipeEnergyCost = 5f;
    private const float MESSAGE_COOLDOWN = 1f;
    private static float _lastMessageTime = 0f;

    private static bool ShowMessage(string str)
    {
        if (_lastMessageTime + MESSAGE_COOLDOWN >= Time.unscaledTime
            && _lastMessageTime <= Time.unscaledTime)
            return false;
        ErrorMessage.AddWarning(str);
        _lastMessageTime = Time.unscaledTime;
        return true;
    }

    public static bool IsGhostCrafterCraftTree(CraftTree.Type type)
    {
        // Skip: None(0), Rocket(8), Constructor(2), SeamothUpgrades(10),
        // Workbench? actually 4 and 5 are probably Cyclops modules. Keep the
        // EasyCraft blacklist identical.
        return type != CraftTree.Type.None
            && type != CraftTree.Type.Rocket
            && type != CraftTree.Type.Constructor
            && type != CraftTree.Type.SeamothUpgrades
            && type != CraftTree.Type.MapRoom
            && type != CraftTree.Type.CyclopsFabricator;
    }

    /// <summary>
    /// Whether the recipe can be crafted recursively from what is in range.
    /// </summary>
    public static bool IsCraftRecipeFulfilledAdvanced(TechType techType)
    {
        if (Inventory.main == null) return false;
        if (!GameModeUtils.RequiresIngredients()) return true;
        var consumable = new Dictionary<TechType, int>();
        var crafted = new Dictionary<TechType, int>();
        return _IsCraftRecipeFulfilledAdvanced(techType, techType, consumable, crafted);
    }

    private static bool _IsCraftRecipeFulfilledAdvanced(
        TechType parent,
        TechType techType,
        Dictionary<TechType, int> consumable,
        Dictionary<TechType, int> crafted,
        int depth = 0)
    {
        if (depth >= 5) return false;
        var ingredients = TechData.GetIngredients(techType);
        if (ingredients == null || ingredients.Count <= 0) return false;
        crafted.Inc(techType);

        foreach (var ing in ingredients)
        {
            var ingTT = ing.techType;
            if (parent == ingTT) return false;
            int pickupCount = ClosestItemContainers.GetPickupCount(ingTT);
            int alreadyConsumed = consumable.TryGetValue(ingTT, out var cv) ? cv : 0;
            int avail = Mathf.Max(0, pickupCount - alreadyConsumed);
            int need = ing.amount;

            if (avail < need)
            {
                var eqType = TechData.GetEquipmentType(ingTT);
                // Do not recursively craft equipment; it is not an auto-craft candidate.
                if (!AutoCraftSettings.AutoCraft
                    || IsEquipmentTypeAutoCraftBlocked(eqType)
                    || AutoCraftSettings.DisabledAutoCraftRecipes.Contains(ingTT))
                    return false;

                consumable.Inc(ingTT, avail);
                for (int i = 0; i < need - avail; i++)
                {
                    if (!_IsCraftRecipeFulfilledAdvanced(parent, ingTT, consumable, crafted, depth + 1)
                        || !ClosestFabricators.CanCraft(ingTT))
                        return false;
                    int craftAmount = TechData.GetCraftAmount(ingTT);
                    if (craftAmount > 0)
                    {
                        i += craftAmount - 1;
                        consumable.Inc(ingTT, -craftAmount);
                        var linked = TechData.GetLinkedItems(ingTT);
                        if (linked != null)
                            foreach (var k in linked) consumable.Inc(k, -1);
                    }
                }
                consumable.Inc(ingTT, need - avail);
                int totalConsumed = consumable.TryGetValue(ingTT, out var tc) ? tc : 0;
                if (pickupCount < totalConsumed) return false;
            }
            else
            {
                consumable.Inc(ingTT, ing.amount);
            }
        }
        return true;
    }

    private static bool IsEquipmentTypeAutoCraftBlocked(EquipmentType t)
    {
        // Same blocks as EasyCraft: BatteryCharger, Hand, Head, Chip, Tank,
        // Fins, PowerCellCharger, Body, Gloves, etc.
        return t == EquipmentType.BatteryCharger
            || t == EquipmentType.Hand
            || t == EquipmentType.Head
            || t == EquipmentType.Chip
            || t == EquipmentType.Tank
            || t == EquipmentType.PowerCellCharger
            || t == EquipmentType.Body
            || t == EquipmentType.Gloves;
    }

    /// <summary>
    /// Core: called instead of vanilla GhostCrafter.Craft. Handles auto-crafting
    /// sub-ingredients, consumes resources, and delegates animation to vanilla
    /// OnCraftingBegin.
    /// </summary>
    public static void GhostCraft(GhostCrafter crafter, TechType techType, float duration)
    {
        QoLLog.Debug(Category.AutoCraft, $"Craft {techType}");
        if (crafter == null) return;

        var powerRelay = crafter.powerRelay;
        float animDelay = crafter.spawnAnimationDelay;
        float animDuration = crafter.spawnAnimationDuration;
        var crafterLogic = crafter.logic;

        // Batch multiplier from Shift/Ctrl. Verify step by step that we can afford
        // N crafts; if not, reduce to the maximum affordable amount.
        int desiredBatch = AutoCraftSettings.GetBatchMultiplier();
        var consumable = new Dictionary<TechType, int>();
        var crafted = new Dictionary<TechType, int>();

        if (GameModeUtils.RequiresIngredients())
        {
            int actualBatch = 0;
            for (int i = 0; i < desiredBatch; i++)
            {
                // Snapshot for rollback when this iteration fails.
                var snapshotConsumable = new Dictionary<TechType, int>(consumable);
                var snapshotCrafted = new Dictionary<TechType, int>(crafted);
                if (!_IsCraftRecipeFulfilledAdvanced(techType, techType, snapshotConsumable, snapshotCrafted))
                    break;
                consumable = snapshotConsumable;
                crafted = snapshotCrafted;
                actualBatch++;
            }
            if (actualBatch == 0)
            {
                ShowMessage(Language.main.Get("DontHaveNeededIngredients"));
                return;
            }
            if (desiredBatch > 1 && actualBatch < desiredBatch)
                QoLLog.Info(Category.AutoCraft,
                    $"Batch scaled down: {techType} x{actualBatch} (wanted x{desiredBatch})");
            desiredBatch = actualBatch;

            foreach (var kvp in crafted)
            {
                float t = 0f;
                if (TechData.GetCraftTime(kvp.Key, out t))
                    duration += t * kvp.Value;
            }
        }
        else
        {
            crafted.Add(techType, desiredBatch);
        }

        duration = Mathf.Clamp(duration, animDelay + animDuration, 20f);

        // Craft speed multiplier: faster = shorter duration (1/mult) + higher
        // consumption (*mult). Minimum duration is still limited by animDelay+animDuration.
        float speedMult = AutoCraftSettings.SpeedMultiplier;
        float originalDuration = duration;
        if (speedMult > 1f)
        {
            duration = Mathf.Max(animDelay + animDuration, duration / speedMult);
        }
        QoLLog.Info(Category.AutoCraft,
            $"{techType}: speed x{speedMult:0.00}, duration {originalDuration:0.00}s -> {duration:0.00}s (min {animDelay + animDuration:0.00}s)");

        if (AutoCraftSettings.AutoCraft && crafted.Count > 1)
        {
            if (AutoCraftSettings.UseStorage == NeighboringStorage.Off)
                ClosestFabricators.Add(crafter);
            if (!ConsumeEnergyInternal(crafted, speedMult)) return;
        }
        else
        {
            float cost = DefaultRecipeEnergyCost;
            TechData.GetEnergyCost(techType, out cost);
            if (cost <= 0) cost = DefaultRecipeEnergyCost;
            // Single recipe: energy = base * batch * speedMult
            float totalCost = cost * desiredBatch * speedMult;
            if (!CrafterLogic.ConsumeEnergy(powerRelay, totalCost))
            {
                if (!ShowMessage(Language.main.Get("NoPower")))
                    return;
                QoLLog.Debug(Category.AutoCraft, $"Not enough energy {totalCost}");
                return;
            }
            QoLLog.Debug(Category.AutoCraft,
                $"Consume energy {totalCost} (batch x{desiredBatch}, speed x{speedMult:0.0})");
        }

        ConsumeIngredients(consumable);

        if (crafterLogic == null || !crafterLogic.Craft(techType, duration)) return;
        QoLLog.Debug(Category.AutoCraft, "Craft permitted");

        // Batch: multiply numCrafted so TryPickupAsync picks up N times more items.
        if (desiredBatch > 1)
        {
            int originalNumCrafted = crafterLogic.numCrafted;
            crafterLogic.numCrafted = originalNumCrafted * desiredBatch;
            QoLLog.Info(Category.AutoCraft,
                $"{techType} batch x{desiredBatch}: numCrafted {originalNumCrafted} -> {crafterLogic.numCrafted}");
        }

        crafter.state = true;
        crafter.OnCraftingBegin(techType, duration);
    }

    public static void ConstructorCraft(ConstructorInput crafter, TechType techType, float duration)
    {
        QoLLog.Debug(Category.AutoCraft, $"Constructor Craft {techType}");
        if (crafter == null) return;

        // ConstructorInput internally uses reflection because of private fields.
        var crafterLogicField = typeof(ConstructorInput)
            .GetProperty("logic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var stateField = typeof(ConstructorInput)
            .GetProperty("state", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var onCraftingBegin = typeof(ConstructorInput)
            .GetMethod("OnCraftingBegin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var returnValidPos = typeof(ConstructorInput)
            .GetMethod("ReturnValidCraftingPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var crafterLogic = crafterLogicField?.GetValue(crafter) as CrafterLogic;

        var spawn = crafter.constructor?.GetItemSpawnPoint(techType);
        if (spawn == null) return;

        if (techType == TechType.Cyclops)
        {
            var res = (bool?)returnValidPos?.Invoke(crafter, new object[] { spawn.position });
            if (res != true)
            {
                crafter.invalidNotification?.Play();
                return;
            }
        }

        duration = 10f;
        int diff = (int)techType - 2000;
        if (diff > 1)
        {
            if (techType == TechType.Cyclops) duration = 20f;
            else if ((int)techType == 5900) duration = 25f;
        }

        var consumable = new Dictionary<TechType, int>();
        var selfCrafted = new Dictionary<TechType, int>();

        if (GameModeUtils.RequiresIngredients())
        {
            if (!_IsCraftRecipeFulfilledAdvanced(techType, techType, consumable, selfCrafted))
            {
                ShowMessage(Language.main.Get("DontHaveNeededIngredients"));
                return;
            }
            selfCrafted.Remove(techType);
            foreach (var kvp in selfCrafted)
            {
                float t = 0f;
                if (TechData.GetCraftTime(kvp.Key, out t))
                    duration += t * kvp.Value;
            }
        }

        duration = Mathf.Clamp(duration, 3f, 50f);

        if (AutoCraftSettings.AutoCraft && selfCrafted.Values.Sum() > 0)
        {
            if (AutoCraftSettings.UseStorage != NeighboringStorage.Range100 && GameModeUtils.RequiresPower())
            {
                if (!ShowMessage(Language.main.Get("NoPower"))) return;
                QoLLog.Debug(Category.AutoCraft, "Not enough energy");
                return;
            }
            if (!ConsumeEnergyInternal(selfCrafted)) return;
        }

        ConsumeIngredients(consumable);

        if (crafterLogic == null || !crafterLogic.Craft(techType, duration)) return;
        QoLLog.Debug(Category.AutoCraft, "Constructor Craft permitted");
        stateField?.SetValue(crafter, true);
        onCraftingBegin?.Invoke(crafter, new object[] { techType, duration });
    }

    public static bool Construct(Constructable construct)
    {
        if (construct == null) return false;
        var resourceMap = (List<TechType>)typeof(Constructable)
            .GetField("resourceMap", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(construct);
        var getResourceId = typeof(Constructable)
            .GetMethod("GetResourceID", BindingFlags.Instance | BindingFlags.NonPublic);
        var updateMaterial = typeof(Constructable)
            .GetMethod("UpdateMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
        var getInterval = typeof(Constructable)
            .GetMethod("GetConstructInterval", BindingFlags.Static | BindingFlags.NonPublic);

        if (resourceMap == null || getResourceId == null || updateMaterial == null || getInterval == null)
            return false;

        float interval = (float)getInterval.Invoke(construct, null);
        if (construct._constructed) return false;

        int count = resourceMap.Count;
        int beforeId = (int)getResourceId.Invoke(construct, null);
        construct.constructedAmount += Time.deltaTime / (count * interval);
        construct.constructedAmount = Mathf.Clamp01(construct.constructedAmount);
        int afterId = (int)getResourceId.Invoke(construct, null);

        if (afterId != beforeId)
        {
            TechType need = resourceMap[afterId - 1];
            if (GameModeUtils.RequiresIngredients())
            {
                if (ClosestItemContainers.GetPickupCount(need) > 0)
                {
                    if (!ClosestItemContainers.DestroyItem(need))
                    {
                        construct.constructedAmount = (float)beforeId / count;
                        return false;
                    }
                    uGUI_IconNotifier.main.Play(need, uGUI_IconNotifier.AnimationType.From, null);
                }
                else if (AutoCraftSettings.AutoCraft && AutoCraftSettings.UseStorage != NeighboringStorage.Off)
                {
                    var innerConsumable = new Dictionary<TechType, int>();
                    var innerCrafted = new Dictionary<TechType, int>();
                    if (!_IsCraftRecipeFulfilledAdvanced(need, need, innerConsumable, innerCrafted)
                        || !ClosestFabricators.CanCraft(need))
                    {
                        construct.constructedAmount = (float)beforeId / count;
                        return false;
                    }
                    if (!ConsumeEnergyInternal(innerCrafted))
                    {
                        construct.constructedAmount = (float)beforeId / count;
                        return false;
                    }
                    int craftAmount = TechData.GetCraftAmount(need);
                    if (craftAmount > 0)
                    {
                        innerConsumable.Inc(need, -(craftAmount - 1));
                        var linked = TechData.GetLinkedItems(need);
                        if (linked != null)
                            foreach (var k in linked) innerConsumable.Inc(k, -1);
                    }
                    ConsumeIngredients(innerConsumable);
                }
                else
                {
                    construct.constructedAmount = (float)beforeId / count;
                    return false;
                }
            }
        }

        updateMaterial.Invoke(construct, null);
        if (construct.constructedAmount >= 1.0f) construct.SetState(true, true);
        return true;
    }

    private static void ConsumeIngredients(Dictionary<TechType, int> consumable)
    {
        if (!GameModeUtils.RequiresIngredients()) return;
        foreach (var kvp in consumable)
        {
            if (kvp.Value > 0)
            {
                ClosestItemContainers.DestroyItem(kvp.Key, kvp.Value);
                uGUI_IconNotifier.main?.Play(kvp.Key, uGUI_IconNotifier.AnimationType.From, null);
            }
        }
        foreach (var kvp in consumable)
        {
            if (kvp.Value < 0)
            {
                ClosestItemContainers.AddItem(kvp.Key, Mathf.Abs(kvp.Value));
                uGUI_IconNotifier.main?.Play(kvp.Key, uGUI_IconNotifier.AnimationType.To, null);
            }
        }
    }

    private static bool ConsumeEnergyInternal(Dictionary<TechType, int> crafted, float speedMult = 1f)
    {
        if (!GameModeUtils.RequiresPower() || crafted.Count == 0) return true;

        // Speed multiplier scales energy. Use a larger crafted dictionary
        // (multiplied) so HasEnergy + ConsumeEnergy account for the higher cost.
        Dictionary<TechType, int> effective = crafted;
        if (speedMult > 1f && speedMult <= 100f)
        {
            effective = new Dictionary<TechType, int>();
            foreach (var kvp in crafted)
                effective[kvp.Key] = Mathf.CeilToInt(kvp.Value * speedMult);
        }

        if (!ClosestFabricators.HasEnergy(effective, out var needEnergy))
        {
            if (ShowMessage(Language.main.Get("NoPower")))
                QoLLog.Debug(Category.AutoCraft, $"Not enough energy {needEnergy}");
            return false;
        }
        if (!ClosestFabricators.ConsumeEnergy(effective, out var consumed))
        {
            if (ShowMessage(Language.main.Get("NoPower")))
                QoLLog.Debug(Category.AutoCraft, "Not enough energy (mid)");
            return false;
        }
        QoLLog.Debug(Category.AutoCraft, $"Consume energy {consumed} (speed x{speedMult:0.0})");
        return true;
    }

    public static void WriteIngredients(IList<Ingredient> ingredients, List<TooltipIcon> icons)
    {
        if (ingredients == null || icons == null) return;
        var sb = new StringBuilder();
        for (int i = 0; i < ingredients.Count; i++)
        {
            sb.Length = 0;
            var ing = ingredients[i];
            int pickupCount = ClosestItemContainers.GetPickupCount(ing.techType);
            int need = ing.amount;
            bool satisfied = pickupCount >= need || !GameModeUtils.RequiresIngredients();
            var sprite = SpriteManager.Get(ing.techType);
            sb.Append(satisfied ? "<color=#94DE00FF>" : "<color=#DF4026FF>");
            var name = TechTypeExtensions.GetOrFallback(Language.main, TooltipFactory.techTypeIngredientStrings.Get(ing.techType), ing.techType);
            sb.Append(name);
            if (need > 1) { sb.Append(" x"); sb.Append(need); }
            if (pickupCount > 0 && pickupCount < need) { sb.Append(" ("); sb.Append(pickupCount); sb.Append(")"); }
            sb.Append("</color>");
            icons.Add(new TooltipIcon(sprite, sb.ToString()));
        }
    }
}

internal static class DictSumExt
{
    public static int Sum<T>(this System.Collections.Generic.Dictionary<T, int>.ValueCollection vals)
    {
        int t = 0;
        foreach (var v in vals) t += v;
        return t;
    }
}
