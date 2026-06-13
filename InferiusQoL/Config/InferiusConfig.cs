namespace InferiusQoL.Config;

using InferiusQoL.Logging;
using InferiusQoL.Features.InventoryStacking;
using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using Newtonsoft.Json;

// LoadOn intentionally omits MenuOpened: reloading every time the menu opened
// caused sliders to disappear because of an incompatibility with our field count.
// Load runs only once at startup, manually via .Load() in Plugin.Awake, and the
// config file updates only on SaveOn.ChangeValue / SaveGame.
[Menu(MyPluginInfo.PLUGIN_NAME,
    SaveOn = MenuAttribute.SaveEvents.ChangeValue | MenuAttribute.SaveEvents.SaveGame)]
public class InferiusConfig : ConfigFile
{
    // =====================================================================
    // General
    // =====================================================================

    [Choice("Debug verbosity", new[] { "None", "Info", "Debug", "Trace" }, Order = 0)]
    public string Verbosity = "Info";

    // =====================================================================
    // Player inventory
    // =====================================================================

    [Toggle("Enlarge player inventory", Order = 100)]
    public bool InventoryResizeEnabled = true;

    [Slider("  Extra rows", 0, 6, DefaultValue = 2, Step = 1, Order = 101)]
    public int InventoryExtraRows = 2;

    [Slider("  Extra columns", 0, 4, DefaultValue = 0, Step = 1, Order = 102)]
    public int InventoryExtraCols = 0;

    [Slider("  ScrollableInventory max visible rows", 4, 12, DefaultValue = 8, Step = 1, Order = 103)]
    public int ScrollableInventoryMaxVisibleRows = 8;

    [Toggle("Enable inventory stacking", Order = 150)]
    public bool InventoryStackingEnabled = true;

    [Slider("  Max stack size", 2, 100, DefaultValue = 10, Step = 1, Order = 151)]
    public int InventoryStackingMaxStackSize = 10;

    [Slider("  Split merge protection (seconds)", 2f, 60f, DefaultValue = 10f, Step = 1f, Order = 152)]
    public float InventoryStackingSplitMergeProtectionSeconds = 10f;

    [Choice("  Split stack modifier", new[] { "LeftAlt", "RightAlt", "LeftControl", "RightControl", "LeftShift", "RightShift" }, Order = 153)]
    public string InventoryStackingSplitPromptKey = "LeftAlt";

    [Choice("  Move half-stack modifier", new[] { "LeftControl", "RightControl", "LeftAlt", "RightAlt", "LeftShift", "RightShift" }, Order = 154)]
    public string InventoryStackingMoveHalfKey = "LeftControl";

    [Choice("  Merge matching stacks modifier", new[] { "LeftShift", "RightShift", "LeftAlt", "RightAlt", "LeftControl", "RightControl" }, Order = 155)]
    public string InventoryStackingMergeAllKey = "LeftShift";

    [Toggle("  Consumables stackable", Order = 156)]
    public bool InventoryStackingConsumablesStackable = false;

    [Toggle("  Vehicle upgrades stackable", Order = 157)]
    public bool InventoryStackingVehicleUpgradesStackable = false;

    [Button("  Restore stacking from backup (up to 100 saves)", Order = 158)]
    public void RestoreInventoryStackingFromBackup(ButtonClickedEventArgs e)
    {
        StackRestorePreviewUi.TryShow();
    }

    // =====================================================================
    // Locker resize
    // =====================================================================

    [Toggle("Bigger locker capacity", Order = 200)]
    public bool LockerResizeEnabled = true;

    [Slider("  Locker width (cols)", 4, 12, DefaultValue = 6, Step = 1, Order = 201)]
    public int LockerWidth = 6;

    [Slider("  Locker height (rows)", 4, 16, DefaultValue = 8, Step = 1, Order = 202)]
    public int LockerHeight = 8;

    [Slider("  Wall locker width", 2, 5, DefaultValue = 4, Step = 1, Order = 203)]
    public int WallLockerWidth = 4;

    [Slider("  Wall locker height", 2, 7, DefaultValue = 5, Step = 1, Order = 204)]
    public int WallLockerHeight = 5;

    [Slider("  Waterproof locker width", 3, 8, DefaultValue = 5, Step = 1, Order = 205)]
    public int WaterproofLockerWidth = 5;

    [Slider("  Waterproof locker height", 4, 10, DefaultValue = 7, Step = 1, Order = 206)]
    public int WaterproofLockerHeight = 7;

    [Slider("  Carryall width", 2, 6, DefaultValue = 3, Step = 1, Order = 207)]
    public int CarryallWidth = 3;

    [Slider("  Carryall height", 3, 8, DefaultValue = 6, Step = 1, Order = 208)]
    public int CarryallHeight = 6;

    [Slider("  Vehicle storage width", 4, 8, DefaultValue = 5, Step = 1, Order = 209)]
    public int VehicleStorageWidth = 5;

    [Slider("  Vehicle storage height", 4, 8, DefaultValue = 6, Step = 1, Order = 210)]
    public int VehicleStorageHeight = 6;

    // =====================================================================
    // Base glass integrity
    // =====================================================================

    [Slider("Glass hull penalty (%)", 0, 100, DefaultValue = 50, Step = 1, Order = 250)]
    public int BaseGlassHullPenaltyPercent = 50;

    [JsonIgnore] public float BaseGlassHullPenaltyMultiplier => BaseGlassHullPenaltyPercent / 100f;

    // =====================================================================
    // Coral regeneration
    // =====================================================================

    [Slider("Coral shell plate regrowth (days)", 1, 10, DefaultValue = 3, Step = 1, Order = 275)]
    public int CoralShellPlateRegrowthDays = 3;

    // =====================================================================
    // Backpacks
    // =====================================================================

    [Toggle("Enable backpacks", Order = 300)]
    public bool BackpacksEnabled = true;

    [Slider("  Small backpack extra rows", 1, 4, DefaultValue = 1, Step = 1, Order = 301)]
    public int BackpackSmallRows = 1;

    [Slider("  Medium backpack extra rows", 1, 6, DefaultValue = 2, Step = 1, Order = 302)]
    public int BackpackMediumRows = 2;

    [Slider("  Large backpack extra rows", 1, 8, DefaultValue = 3, Step = 1, Order = 303)]
    public int BackpackLargeRows = 3;

    // =====================================================================
    // Seamoth turbo
    // =====================================================================

    [Toggle("Enable Seamoth Turbo modules", Order = 400)]
    public bool SeamothTurboEnabled = true;

    // Integer percentage to avoid Nautilus float serialization bug. 100 = 1.00x.
    [Slider("  MK1 speed (%)", 100, 500, DefaultValue = 200, Step = 10, Order = 401)]
    public int SeamothTurboMK1SpeedPercent = 200;

    [Slider("  MK1 energy drain (%)", 100, 1500, DefaultValue = 450, Step = 25, Order = 402)]
    public int SeamothTurboMK1EnergyPercent = 450;

    [Slider("  MK2 speed (%)", 100, 600, DefaultValue = 330, Step = 10, Order = 403)]
    public int SeamothTurboMK2SpeedPercent = 330;

    [Slider("  MK2 energy drain (%)", 100, 2000, DefaultValue = 900, Step = 25, Order = 404)]
    public int SeamothTurboMK2EnergyPercent = 900;

    [Slider("  MK3 speed (%)", 100, 700, DefaultValue = 430, Step = 10, Order = 405)]
    public int SeamothTurboMK3SpeedPercent = 430;

    [Slider("  MK3 energy drain (%)", 100, 3500, DefaultValue = 1800, Step = 50, Order = 406)]
    public int SeamothTurboMK3EnergyPercent = 1800;

    [Slider("  Surface falloff distance (m)", 0, 60, DefaultValue = 30, Step = 1, Order = 407)]
    public int SeamothTurboSurfaceFalloffMeters = 30;

    [JsonIgnore] public float SeamothTurboMK1SpeedMultiplier => SeamothTurboMK1SpeedPercent / 100f;
    [JsonIgnore] public float SeamothTurboMK1EnergyMultiplier => SeamothTurboMK1EnergyPercent / 100f;
    [JsonIgnore] public float SeamothTurboMK2SpeedMultiplier => SeamothTurboMK2SpeedPercent / 100f;
    [JsonIgnore] public float SeamothTurboMK2EnergyMultiplier => SeamothTurboMK2EnergyPercent / 100f;
    [JsonIgnore] public float SeamothTurboMK3SpeedMultiplier => SeamothTurboMK3SpeedPercent / 100f;
    [JsonIgnore] public float SeamothTurboMK3EnergyMultiplier => SeamothTurboMK3EnergyPercent / 100f;

    // =====================================================================
    // Retriever
    // =====================================================================

    [Toggle("Enable Retriever terminal", Order = 500)]
    public bool RetrieverEnabled = true;

    [Slider("  Cost per retrieval (J)", 0, 50, DefaultValue = 5, Step = 1, Order = 501)]
    public int RetrieverActionCostJoules = 5;

    [Slider("  Min base power (%)", 0, 100, DefaultValue = 20, Step = 5, Order = 502)]
    public int RetrieverMinBasePowerPercent = 20;

    // =====================================================================
    // Compressor
    // =====================================================================

    [Toggle("Enable Compressor (item press)", Order = 600)]
    public bool CompressorEnabled = true;

    // If false, the Compressor chip is not in the craft tree and is not in the PDA.
    // The TechType is still registered so `spawn InferiusCompressor` works in the
    // console. The feature is paused; existing compressed items still decompress.
    [Toggle("  Craftable (Experimental)", Order = 603)]
    public bool CompressorCraftable = false;

    [Toggle("  Requires energy to compress", Order = 601)]
    public bool CompressorRequiresEnergy = true;

    [Slider("  Energy per compression (J)", 0, 100, DefaultValue = 10, Step = 1, Order = 602)]
    public int CompressorEnergyCost = 10;

    // =====================================================================
    // Tank Welder
    // =====================================================================

    [Toggle("Enable Tank Welder", Order = 700)]
    public bool TankWelderEnabled = true;

    // Integer percentage to avoid Nautilus float serialization bug.
    [Slider("  Tier 1 multiplier (%)", 100, 250, DefaultValue = 100, Step = 10, Order = 701)]
    public int TankWelderT1Percent = 100;

    [Slider("  Tier 2 multiplier (%)", 100, 250, DefaultValue = 125, Step = 5, Order = 702)]
    public int TankWelderT2Percent = 125;

    [Slider("  Tier 3 multiplier (%)", 100, 250, DefaultValue = 150, Step = 5, Order = 703)]
    public int TankWelderT3Percent = 150;

    [Slider("  Tier 4 multiplier (%)", 100, 250, DefaultValue = 150, Step = 5, Order = 704)]
    public int TankWelderT4Percent = 150;

    [JsonIgnore] public float TankWelderT1Multiplier => TankWelderT1Percent / 100f;
    [JsonIgnore] public float TankWelderT2Multiplier => TankWelderT2Percent / 100f;
    [JsonIgnore] public float TankWelderT3Multiplier => TankWelderT3Percent / 100f;
    [JsonIgnore] public float TankWelderT4Multiplier => TankWelderT4Percent / 100f;

    // =====================================================================
    // Batteries rework
    // =====================================================================

    [Toggle("Enable battery rework", Order = 800)]
    public bool BatteryReworkEnabled = true;

    [Slider("  Reinforced Battery capacity", 100, 500, DefaultValue = 250, Step = 10, Order = 801)]
    public int ReinforcedBatteryCapacity = 250;

    [Slider("  Reinforced Power Cell capacity", 200, 1000, DefaultValue = 500, Step = 20, Order = 802)]
    public int ReinforcedPowerCellCapacity = 500;

    [Slider("  Hyper Battery capacity", 1000, 3000, DefaultValue = 1500, Step = 50, Order = 803)]
    public int HyperBatteryCapacity = 1500;

    [Slider("  Hyper Power Cell capacity", 2000, 6000, DefaultValue = 3000, Step = 100, Order = 804)]
    public int HyperPowerCellCapacity = 3000;

    // =====================================================================
    // Teleport Beacon
    // =====================================================================

    [Toggle("Enable Teleport Beacon", Order = 900)]
    public bool TeleportBeaconEnabled = true;

    [Slider("  Source base cost (J)", 0, 5000, DefaultValue = 250, Step = 25, Order = 901)]
    public int TeleportSourceCostJoules = 250;

    [Slider("  Target base cost (J)", 0, 5000, DefaultValue = 250, Step = 25, Order = 902)]
    public int TeleportTargetCostJoules = 250;

    [Slider("  Min power on both bases (%)", 0, 100, DefaultValue = 30, Step = 5, Order = 903)]
    public int TeleportMinBasePowerPercent = 30;

    [Slider("  Cooldown (s)", 0, 300, DefaultValue = 20, Step = 5, Order = 904)]
    public int TeleportCooldownSeconds = 20;

    [Slider("  Energy cost per 100m (J)", 0, 500, DefaultValue = 40, Step = 5, Order = 905)]
    public int TeleportCostPerHundredMeters = 40;

    [Slider("  Efficiency chip MK1 cost (%)", 10, 100, DefaultValue = 75, Step = 5, Order = 906)]
    public int TeleportEfficiencyMK1Percent = 75;

    [Slider("  Efficiency chip MK2 cost (%)", 10, 100, DefaultValue = 50, Step = 5, Order = 907)]
    public int TeleportEfficiencyMK2Percent = 50;

    [Slider("  Efficiency chip MK3 cost (%)", 5, 100, DefaultValue = 25, Step = 5, Order = 908)]
    public int TeleportEfficiencyMK3Percent = 25;

    // Creative/Freedom mode = teleport is automatically free because vanilla does
    // not require power. Survival/Hardcore normally pay, but the toggle makes it free.
    [Toggle("  Always free (no energy cost)", Order = 909)]
    public bool TeleportAlwaysFree = false;

    public float GetEfficiencyMultiplier(int tier) => tier switch
    {
        1 => TeleportEfficiencyMK1Percent / 100f,
        2 => TeleportEfficiencyMK2Percent / 100f,
        3 => TeleportEfficiencyMK3Percent / 100f,
        _ => 1f,
    };

    // =====================================================================
    // Locker Mover (moving full lockers)
    // =====================================================================

    [Toggle("Enable Locker Mover", Order = 1000)]
    public bool LockerMoverEnabled = true;

    // KeyCode name, see UnityEngine.KeyCode. "G" is the default. Later we can
    // make a proper dropdown; for now string configuration is sufficient.
    [Choice("  Keybind",
        new[] { "G", "H", "J", "K", "U", "P", "X", "B", "V", "N", "M" },
        Order = 1001)]
    public string LockerMoverKey = "G";

    [Toggle("  Require empty hands", Order = 1002)]
    public bool LockerMoverRequireEmptyHands = false;

    // =====================================================================
    // Inventory Viewer (aggregate overview across containers)
    // =====================================================================

    [Toggle("Enable Inventory Viewer", Order = 1300)]
    public bool InventoryViewerEnabled = true;

    [Choice("  Toggle key",
        new[] { "I", "J", "K", "L", "M", "N", "O", "P", "U", "Y" },
        Order = 1301)]
    public string InventoryViewerKey = "I";

    [Slider("  Scan range (m)", 0, 500, DefaultValue = 100, Step = 10, Order = 1302)]
    public int InventoryViewerRangeMeters = 100;

    [Toggle("  Include player inventory", Order = 1303)]
    public bool InventoryViewerIncludePlayer = true;

    // =====================================================================
    // AutoCraft (EasyCraft integration)
    // =====================================================================

    [Toggle("Enable AutoCraft", Order = 1100)]
    public bool AutoCraftEnabled = true;

    // Range first = default selection in the Nautilus Choice dropdown.
    [Choice("  Use nearby storage", new[] { "Range", "Inside base/pod", "Off" }, Order = 1101)]
    public string AutoCraftUseStorage = "Range";

    [Slider("  Range (m)", 50, 500, DefaultValue = 100, Step = 10, Order = 1102)]
    public int AutoCraftRangeMeters = 100;

    [Choice("  Return surplus to", new[] { "Inventory", "Lockers" }, Order = 1103)]
    public string AutoCraftReturnSurplus = "Inventory";

    [Toggle("  Better ingredient tooltips", Order = 1104)]
    public bool AutoCraftBetterTooltips = true;

    [Slider("  Shift multiplier (batch)", 1, 20, DefaultValue = 5, Step = 1, Order = 1105)]
    public int AutoCraftShiftMultiplier = 5;

    [Slider("  Ctrl multiplier (batch)", 1, 50, DefaultValue = 10, Step = 1, Order = 1106)]
    public int AutoCraftCtrlMultiplier = 10;

    // 100 = vanilla speed + consumption. 200 = 2x faster + 2x consumption. Scales
    // duration (1/mult) and energy cost (mult).
    [Slider("  Craft speed (%)", 50, 500, DefaultValue = 100, Step = 10, Order = 1107)]
    public int AutoCraftSpeedPercent = 100;

    // =====================================================================
    // Oxygen Auto-Refill
    // =====================================================================

    [Toggle("Enable faster oxygen refill", Order = 1200)]
    public bool OxygenRefillEnabled = true;

    // Vanilla = 30 units/sec when surfaced or in a base. Higher = faster tank refill.
    [Slider("  Refill rate (units/sec)", 30, 300, DefaultValue = 120, Step = 10, Order = 1201)]
    public int OxygenRefillRate = 120;

    [Toggle("  Refill all tanks in inventory", Order = 1202)]
    public bool OxygenRefillInventoryTanks = true;

    // =====================================================================
    // Scanner Room
    // =====================================================================

    [Toggle("Scanner room drillables and time capsules", Order = 1400)]
    public bool ScannerRoomDrillableScanEnabled = true;

    // =====================================================================
    // Mobile Resource Scanner
    // =====================================================================

    [Toggle("Enable Mobile Resource Scanner", Order = 1450)]
    public bool MobileResourceScannerEnabled = true;

    [Toggle("  Require PDA scan entries", Order = 1451)]
    public bool MobileResourceScannerRequireScanned = false;

    [Toggle("  Show every TechType", Order = 1452)]
    public bool MobileResourceScannerShowAllTechTypes = false;

    [Slider("  Range (m)", 10, 1000, DefaultValue = 500, Step = 10, Order = 1453)]
    public int MobileResourceScannerRangeMeters = 500;

    [Slider("  Scan interval (s)", 1, 60, DefaultValue = 10, Step = 1, Order = 1454)]
    public int MobileResourceScannerIntervalSeconds = 10;

    [Slider("  Scanner energy use (%)", 0, 200, DefaultValue = 100, Step = 5, Order = 1455)]
    public int MobileResourceScannerEnergyUsePercent = 100;

    [Choice("  Open menu modifier",
        new[] { "None", "Shift", "Ctrl", "Alt", "LeftShift", "RightShift", "LeftCtrl", "RightCtrl", "LeftAlt", "RightAlt" },
        Order = 1456)]
    public string MobileResourceScannerMenuModifier = "None";

    [Button("  Reset selected resource", Order = 1457)]
    public void ResetMobileResourceScannerResource(ButtonClickedEventArgs e)
    {
        MobileResourceScannerCurrentResource = TechType.None.ToString();
        Save();
        InferiusQoL.Features.MobileResourceScanner.MobileResourceScannerFeature.SetCurrentResource(TechType.None);
    }

    public string MobileResourceScannerCurrentResource = "None";

    // =====================================================================
    // Singleton
    // =====================================================================

    public static InferiusConfig Instance { get; } = OptionsPanelHandler.RegisterModOptions<InferiusConfig>();

    // Nautilus attribute-based ConfigFile does not call On<Field>Changed handlers,
    // which belong to the imperative API convention. Runtime reactions to Options
    // menu changes are handled by the ConfigSavePatch Harmony patch, which hooks
    // ConfigFile.Save and then calls feature ApplyRuntime methods.
}
