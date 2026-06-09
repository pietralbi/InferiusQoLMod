namespace InferiusQoL;

using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Console;
using InferiusQoL.Features.Backpacks;
using InferiusQoL.Features.Batteries;
using InferiusQoL.Features.Compressor;
using InferiusQoL.Features.Flares;
using InferiusQoL.Features.InventoryStacking;
using InferiusQoL.Features.InventoryViewer;
using InferiusQoL.Features.LockerMover;
using InferiusQoL.Features.ScannerRoom;
using InferiusQoL.Features.SeamothTurbo;
using InferiusQoL.Features.TankWelder;
using InferiusQoL.Features.TeleportBeacon;
using InferiusQoL.Localization;
using InferiusQoL.Logging;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, Nautilus.PluginInfo.PLUGIN_VERSION)]
[BepInIncompatibility("com.ahk1221.smlhelper")]
#if SUBNAUTICA
[BepInProcess("Subnautica.exe")]
#elif BELOWZERO
[BepInProcess("SubnauticaZero.exe")]
#endif
public class Plugin : BaseUnityPlugin
{
    public const string HarmonyId = MyPluginInfo.PLUGIN_GUID;

    internal static Plugin Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony Harmony { get; } = new Harmony(HarmonyId);

    internal static bool HasCustomizedStorage { get; private set; }
    internal static bool HasAdvancedInventory { get; private set; }
    internal static bool HasInventoryStacking { get; private set; }
    internal static bool HasBagEquipment { get; private set; }
    internal static bool HasSlotExtender { get; private set; }
    internal static bool HasEasyCraft { get; private set; }
    internal static bool HasRadialMenu { get; private set; }

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        var cfg = InferiusConfig.Instance;
        cfg.Load();

        QoLLog.Initialize(Logger, cfg.Verbosity);
        QoLLog.Info(Category.Core, $"Loading {MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}");
        QoLLog.Info(Category.Config, "Config loaded");

        L.LoadAll();
        QoLLog.Info(Category.Config, "Localization loaded");

        if (cfg.ScannerRoomDrillableScanEnabled)
            DrillableScanFeature.EnsureTimeCapsuleWorldEntity();

        ConsoleCommands.Register();
        QoLLog.Info(Category.Core, "Console commands registered");

        // Eager radial-menu detection: DetectExternalMods runs in Start(), but
        // tab registration in Awake needs to know whether to create tabs or
        // place upgrades directly in the root. Workbench without a radial menu
        // covers tabs. The eager check may not see every plugin, but radial menu
        // mods usually load early.
        HasRadialMenu = FindPlugin(new[] { "radialtabs", "radialcrafting", "bettercraftmenu", "guibetter" }, out var rmEarlyInfo);
        QoLLog.Info(Category.Core, $"Eager radial menu detection: {(HasRadialMenu ? rmEarlyInfo : "not found, will flatten Workbench tabs")}");

        // Register custom TechTypes in Awake, before the game creates the craft tree.
        if (cfg.SeamothTurboEnabled)
            SeamothTurboItems.Register();

        if (cfg.BackpacksEnabled && !HasBagEquipment)
        {
            // BagEquipment detection runs later in Start(), so register
            // optimistically here. If BagEquipment is detected later, our
            // backpacks still exist in the craft tree but do not apply because
            // InventoryResizePatch.ApplyTo gates them.
            BackpackItems.RegisterTabs();
            BackpackItems.Register();
        }

        if (cfg.TankWelderEnabled)
        {
            TankWelderItems.RegisterTabs();
            TankWelderItems.Register();
        }

        if (cfg.BatteryReworkEnabled)
        {
            BatteryItems.RegisterTabs();
            BatteryItems.Register();
        }

        if (cfg.CompressorEnabled)
        {
            CompressorBlacklist.LoadFromJson();
            CompressorSaveManager.Load();
            CompressorItem.RegisterTabs();
            CompressorItem.Register();
        }

        if (cfg.TeleportBeaconEnabled)
        {
            TeleportBeaconSaveManager.Load();
            TeleportBeaconItem.Register();
            TeleportEfficiencyChips.Register();
        }

        if (cfg.LockerMoverEnabled)
        {
            LockerMoverFeature.Init();
        }

        if (cfg.InventoryViewerEnabled)
        {
            InventoryViewerFeature.Init();
        }

        QoLLog.Info(Category.Core, "Awake completed (external mod detection will run in Start())");
    }

    private void Start()
    {
        // Start() runs after every other plugin's Awake(), so Chainloader.PluginInfos
        // is complete. Detection in Awake is incomplete because some plugins load later.
        DetectExternalMods();
        FlareLifetimeFeature.Init();
        InventoryStackingFeature.Init();

        // Per-class try/catch instead of one large Harmony.PatchAll.
        // If one patch fails, such as when the target method does not exist,
        // the other patches still apply. Previously: one PatchAll -> one exception ->
        // the rest of the patches never ran -> Compressor + InventoryResize failed
        // silently -> compressed items were lost because they did not fit at vanilla size.
        int ok = 0, failed = 0;
        foreach (var type in typeof(Plugin).Assembly.GetTypes())
        {
            if (!type.IsClass) continue;
            if (!InventoryStackingFeature.IsEnabled
                && type.FullName?.StartsWith("InferiusQoL.Features.InventoryStacking.", System.StringComparison.Ordinal) == true)
                continue;

            try
            {
                var processor = Harmony.CreateClassProcessor(type);
                var patched = processor.Patch();
                if (patched != null && patched.Count > 0) ok++;
            }
            catch (System.Exception ex)
            {
                failed++;
                QoLLog.Error(Category.Core,
                    $"Harmony patch failed for {type.FullName}: {ex.Message}", ex);
            }
        }
        QoLLog.Info(Category.Core, $"Harmony patches applied (ok={ok}, failed={failed})");
        InventoryStackingFeature.AfterHarmonyPatched();
    }

    private static void DetectExternalMods()
    {
        HasCustomizedStorage = FindPlugin(new[] { "customizedstorage" }, out var csInfo);
        HasAdvancedInventory = FindPlugin(new[] { "advancedinventory" }, out var aiInfo);
        HasInventoryStacking = FindPlugin(new[] { "mades.redo.inventorystacking", "mr_inventorystacking", "inventory stacking" }, out var stackInfo);
        HasBagEquipment = FindPlugin(new[] { "bagequipment" }, out var beInfo);
        HasSlotExtender = FindPlugin(new[] { "slotextender" }, out var seInfo);
        HasEasyCraft = FindPlugin(new[] { "easycraft" }, out var ecInfo);
        HasRadialMenu = FindPlugin(new[] { "radialtabs", "radialcrafting", "bettercraftmenu", "guibetter" }, out var rmInfo);

        LogDetection("CustomizedStorage", "locker resize", HasCustomizedStorage, csInfo);
        LogDetection("AdvancedInventory", "scrollable container", HasAdvancedInventory, aiInfo);
        LogDetection("MR_InventoryStacking", "inventory stacking", HasInventoryStacking, stackInfo);
        LogDetection("BagEquipment", "backpacks", HasBagEquipment, beInfo);
        // EasyCraft: we have our own port (AutoCraft). If the original EasyCraft
        // is installed, warn the user that they should uninstall the original.
        if (HasEasyCraft)
            QoLLog.Warning(Category.Core,
                $"Original EasyCraft detected: {ecInfo}. Our AutoCraft is an EasyCraft port - "
                + "uninstall the original EasyCraft so the patches do not conflict.");
        else
            QoLLog.Info(Category.Core, "EasyCraft not detected (our AutoCraft port is active).");
        if (HasSlotExtender)
            QoLLog.Info(Category.Core, $"SlotExtender detected: {seInfo} - extra Chip slots available for backpacks.");
        else
            QoLLog.Info(Category.Core, "SlotExtender not detected - backpack will occupy your single vanilla Chip slot (trade-off with Compass).");
    }

    private static bool FindPlugin(string[] needles, out string info)
    {
        foreach (var kvp in Chainloader.PluginInfos)
        {
            var guid = kvp.Key?.ToLowerInvariant() ?? "";
            var meta = kvp.Value?.Metadata;
            var name = meta?.Name?.ToLowerInvariant() ?? "";
            var asmName = kvp.Value?.Instance?.GetType().Assembly.GetName().Name?.ToLowerInvariant() ?? "";
            foreach (var needle in needles)
            {
                var n = needle.ToLowerInvariant();
                if (guid.Contains(n) || name.Contains(n) || asmName.Contains(n))
                {
                    info = $"{meta?.Name} (guid={kvp.Key}) v{meta?.Version}";
                    return true;
                }
            }
        }
        info = "";
        return false;
    }

    private static void LogDetection(string label, string featureDesc, bool found, string info)
    {
        if (found)
            QoLLog.Warning(Category.Core,
                $"{label} detected: {info} -> our {featureDesc} feature will not enable (conflict).");
        else
            QoLLog.Info(Category.Core, $"{label} not detected.");
    }
}
