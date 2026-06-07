namespace InferiusQoL.Features.ScannerRoom;

using System;
using System.Collections.Generic;
using HarmonyLib;
using InferiusQoL.Assets;
using InferiusQoL.Config;
using InferiusQoL.Logging;

internal static class DrillableScanFeature
{
    private const string DrillableSuffixKey = "InferiusQoL.ScannerRoom.DrillableSuffix";
    private const string TimeCapsuleIconFile = "TimeCapsuleScanner.png";

    private static readonly Dictionary<TechType, (string IconFile, TechType FallbackTechType)> DrillableIcons = new Dictionary<TechType, (string IconFile, TechType FallbackTechType)>
    {
        { TechType.DrillableSalt, ("DrillableSalt.png", TechType.Salt) },
        { TechType.DrillableQuartz, ("DrillableQuartz.png", TechType.Quartz) },
        { TechType.DrillableCopper, ("DrillableCopper.png", TechType.Copper) },
        { TechType.DrillableTitanium, ("DrillableTitanium.png", TechType.Titanium) },
        { TechType.DrillableLead, ("DrillableLead.png", TechType.Lead) },
        { TechType.DrillableSilver, ("DrillableSilver.png", TechType.Silver) },
        { TechType.DrillableDiamond, ("DrillableDiamond.png", TechType.Diamond) },
        { TechType.DrillableGold, ("DrillableGold.png", TechType.Gold) },
        { TechType.DrillableMagnetite, ("DrillableMagnetite.png", TechType.Magnetite) },
        { TechType.DrillableLithium, ("DrillableLithium.png", TechType.Lithium) },
        { TechType.DrillableUranium, ("DrillableUranium.png", TechType.Uranium) },
        { TechType.DrillableAluminiumOxide, ("DrillableAluminiumOxide.png", TechType.AluminumOxide) },
        { TechType.DrillableNickel, ("DrillableNickel.png", TechType.Nickel) },
        { TechType.DrillableKyanite, ("DrillableKyanite.png", TechType.Kyanite) },
    };

    private static readonly HashSet<string> DrillableLanguageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        nameof(TechType.DrillableSalt),
        nameof(TechType.DrillableQuartz),
        nameof(TechType.DrillableCopper),
        nameof(TechType.DrillableTitanium),
        nameof(TechType.DrillableLead),
        nameof(TechType.DrillableSilver),
        nameof(TechType.DrillableDiamond),
        nameof(TechType.DrillableGold),
        nameof(TechType.DrillableMagnetite),
        nameof(TechType.DrillableLithium),
        nameof(TechType.DrillableUranium),
        nameof(TechType.DrillableAluminiumOxide),
        nameof(TechType.DrillableNickel),
        nameof(TechType.DrillableKyanite),
    };

    internal static bool Enabled => InferiusConfig.Instance.ScannerRoomDrillableScanEnabled;

    internal static bool IsDrillable(TechType techType) => DrillableIcons.ContainsKey(techType);

    internal static bool IsDrillableLanguageKey(string key)
    {
        return !string.IsNullOrEmpty(key) && DrillableLanguageKeys.Contains(key);
    }

    internal static string AddDrillableSuffix(Language language, string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var suffix = GetDrillableSuffix(language);
        var formattedSuffix = " " + suffix;
        return value.EndsWith(formattedSuffix, StringComparison.Ordinal) ? value : value + formattedSuffix;
    }

    private static string GetDrillableSuffix(Language language)
    {
        if (language != null
            && language.TryGet(DrillableSuffixKey, out var suffix)
            && !string.IsNullOrEmpty(suffix))
        {
            return suffix;
        }

        return "Deposit";
    }

    internal static void EnsureTimeCapsuleDetectable()
    {
        EnsureTimeCapsuleWorldEntity();
        ResourceTrackerDatabase.undetectableTechTypes?.Remove(TechType.TimeCapsule);
        ResourceTrackerDatabase.detectableTechTypes?.Add(TechType.TimeCapsule);
    }

    internal static void EnsureTimeCapsuleWorldEntity()
    {
        var classId = CraftData.GetClassIdForTechType(TechType.TimeCapsule);
        if (string.IsNullOrEmpty(classId))
            return;

        if (!UWE.WorldEntityDatabase.TryGetInfo(classId, out var info))
            return;

        if (info.cellLevel == LargeWorldEntity.CellLevel.VeryFar)
            return;

        info.cellLevel = LargeWorldEntity.CellLevel.VeryFar;
        var database = UWE.WorldEntityDatabase.main;
        if (database?.infos == null)
            return;

        database.infos[classId] = info;
        QoLLog.Trace(Category.ScannerRoom, "Set time capsule world entity cell level to VeryFar");
    }

    internal static void EnsureTimeCapsuleTracker(TimeCapsule timeCapsule)
    {
        if (timeCapsule == null)
            return;

        var techTag = timeCapsule.GetComponent<TechTag>();
        if (techTag == null)
            techTag = timeCapsule.gameObject.AddComponent<TechTag>();

        techTag.type = TechType.TimeCapsule;

        var tracker = timeCapsule.GetComponent<ResourceTracker>();
        if (tracker == null)
            tracker = timeCapsule.gameObject.AddComponent<ResourceTracker>();

        tracker.prefabIdentifier = timeCapsule.GetComponent<PrefabIdentifier>();
        tracker.techType = TechType.TimeCapsule;
        tracker.overrideTechType = TechType.TimeCapsule;
        tracker.rb = timeCapsule.gameObject.GetComponent<UnityEngine.Rigidbody>();
        tracker.pickupable = timeCapsule.gameObject.GetComponent<Pickupable>();
    }

    internal static void UnregisterTimeCapsule(TimeCapsule timeCapsule)
    {
        if (timeCapsule == null)
            return;

        var tracker = timeCapsule.GetComponent<ResourceTracker>();
        if (tracker == null)
            return;

        tracker.Unregister();
    }

    internal static void ApplyScannerIcon(uGUI_Icon icon, TechType techType)
    {
        if (icon == null)
            return;

        if (techType == TechType.TimeCapsule)
        {
            icon.sprite = IconLoader.LoadOrFallback(TimeCapsuleIconFile, TechType.TimeCapsule);
            icon.enabled = true;
            return;
        }

        if (DrillableIcons.TryGetValue(techType, out var iconInfo))
        {
            icon.sprite = IconLoader.LoadOrFallback(iconInfo.IconFile, iconInfo.FallbackTechType);
            icon.enabled = true;
        }
    }
}

[HarmonyPatch(typeof(ResourceTrackerDatabase), nameof(ResourceTrackerDatabase.Start))]
internal static class ScannerRoom_ResourceTrackerDatabase_Start_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!DrillableScanFeature.Enabled)
            return;

        DrillableScanFeature.EnsureTimeCapsuleDetectable();
    }
}

[HarmonyPatch(typeof(TimeCapsule), nameof(TimeCapsule.Start))]
internal static class ScannerRoom_TimeCapsule_Start_Patch
{
    [HarmonyPostfix]
    public static void Postfix(TimeCapsule __instance)
    {
        if (!DrillableScanFeature.Enabled)
            return;

        DrillableScanFeature.EnsureTimeCapsuleDetectable();
        DrillableScanFeature.EnsureTimeCapsuleTracker(__instance);
    }
}

[HarmonyPatch(typeof(TimeCapsule), nameof(TimeCapsule.Collect))]
internal static class ScannerRoom_TimeCapsule_Collect_Patch
{
    [HarmonyPrefix]
    public static void Prefix(TimeCapsule __instance)
    {
        if (!DrillableScanFeature.Enabled)
            return;

        DrillableScanFeature.UnregisterTimeCapsule(__instance);
    }
}

[HarmonyPatch(typeof(ResourceTracker), nameof(ResourceTracker.Start))]
internal static class ScannerRoom_ResourceTracker_Start_Patch
{
    [HarmonyPrefix]
    public static void Prefix(ResourceTracker __instance)
    {
        if (!DrillableScanFeature.Enabled || __instance == null)
            return;

        var techType = CraftData.GetTechType(__instance.gameObject);
        if (!DrillableScanFeature.IsDrillable(techType))
            return;

        __instance.overrideTechType = TechType.None;
        QoLLog.Trace(Category.ScannerRoom, $"Cleared ResourceTracker override for {techType}");
    }
}

[HarmonyPatch(typeof(Language), nameof(Language.Get))]
internal static class ScannerRoom_Language_Get_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Language __instance, ref string __result, string key)
    {
        if (!DrillableScanFeature.Enabled || !DrillableScanFeature.IsDrillableLanguageKey(key))
            return true;

        if (!__instance.TryGet(key, out var value))
            return true;

        __result = DrillableScanFeature.AddDrillableSuffix(__instance, value);
        return false;
    }
}

[HarmonyPatch(typeof(uGUI_MapRoomResourceNode), nameof(uGUI_MapRoomResourceNode.SetTechType))]
internal static class ScannerRoom_MapRoomResourceNode_SetTechType_Patch
{
    [HarmonyPostfix]
    public static void Postfix(uGUI_MapRoomResourceNode __instance, TechType techType)
    {
        if (!DrillableScanFeature.Enabled || __instance == null)
            return;

        DrillableScanFeature.ApplyScannerIcon(__instance.icon, techType);
    }
}

[HarmonyPatch(typeof(uGUI_MapRoomScanner), nameof(uGUI_MapRoomScanner.UpdateGUIState))]
internal static class ScannerRoom_MapRoomScanner_UpdateGUIState_Patch
{
    [HarmonyPostfix]
    public static void Postfix(uGUI_MapRoomScanner __instance)
    {
        if (!DrillableScanFeature.Enabled || __instance == null || __instance.mapRoom == null)
            return;

        DrillableScanFeature.ApplyScannerIcon(__instance.scanningIcon, __instance.mapRoom.GetActiveTechType());
    }
}
