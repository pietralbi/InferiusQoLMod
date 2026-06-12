namespace InferiusQoL.Console;

using System.Text;
using InferiusQoL.Config;
using InferiusQoL.Features.Batteries;
using InferiusQoL.Localization;
using InferiusQoL.Logging;
using Nautilus.Commands;
using Nautilus.Handlers;

public static class ConsoleCommands
{
    public static void Register()
    {
        ConsoleCommandsHandler.RegisterConsoleCommands(typeof(ConsoleCommands));
    }

    [ConsoleCommand("qol_apply")]
    public static string QolApply()
    {
        InferiusQoL.Features.InventoryResize.InventoryResizePatch.ApplyRuntime();
        BatteryItems.ApplyRuntime();
        return "Applied runtime config. Note: some changes still require restart (locker resize, custom item registration).";
    }

    [ConsoleCommand("qol_status")]
    public static string QolStatus()
    {
        var c = InferiusConfig.Instance;
        var sb = new StringBuilder();
        sb.AppendLine(L.Get("InferiusQoL.Status.Header", MyPluginInfo.PLUGIN_VERSION));
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.Verbosity", QoLLog.CurrentVerbosity)}");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.CustomizedStorage", Plugin.HasCustomizedStorage)}");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.AdvancedInventory", Plugin.HasAdvancedInventory)}");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.InventoryStackingExternal", Plugin.HasInventoryStacking)}");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.BagEquipment", Plugin.HasBagEquipment)}");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.EasyCraft", Plugin.HasEasyCraft)}");
        sb.AppendLine($"  SlotExtender detected:      {Plugin.HasSlotExtender}");
        sb.AppendLine(L.Get("InferiusQoL.Status.Features"));
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.InventoryResize"),-18} {OnOff(c.InventoryResizeEnabled)} (+{c.InventoryExtraRows}R/+{c.InventoryExtraCols}C)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.ScrollableInventory"),-18} {(Plugin.HasAdvancedInventory ? "external AdvancedInventory" : OnOff(true))} (max visible rows {c.ScrollableInventoryMaxVisibleRows})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.InventoryStacking"),-18} {(Plugin.HasInventoryStacking ? "external MR_InventoryStacking" : OnOff(c.InventoryStackingEnabled))} (max stack {c.InventoryStackingMaxStackSize})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.LockerResize"),-18} {OnOff(c.LockerResizeEnabled)} ({c.LockerWidth}x{c.LockerHeight}, wall {c.WallLockerWidth}x{c.WallLockerHeight})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.BaseGlass"),-18} {c.BaseGlassHullPenaltyMultiplier:0.##}x penalty");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.Backpacks"),-18} {OnOff(c.BackpacksEnabled)} (S/M/L rows = {c.BackpackSmallRows}/{c.BackpackMediumRows}/{c.BackpackLargeRows})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.SeamothTurbo"),-18} {OnOff(c.SeamothTurboEnabled)} (MK1 {c.SeamothTurboMK1SpeedMultiplier:0.0}x/{c.SeamothTurboMK1EnergyMultiplier:0.0}x, MK2 {c.SeamothTurboMK2SpeedMultiplier:0.0}x/{c.SeamothTurboMK2EnergyMultiplier:0.0}x, MK3 {c.SeamothTurboMK3SpeedMultiplier:0.0}x/{c.SeamothTurboMK3EnergyMultiplier:0.0}x)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.Retriever"),-18} {OnOff(c.RetrieverEnabled)} ({c.RetrieverActionCostJoules} J/item, min {c.RetrieverMinBasePowerPercent}% power)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.Compressor"),-18} {OnOff(c.CompressorEnabled)} (energy: {(c.CompressorRequiresEnergy ? c.CompressorEnergyCost + " J" : "off")})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.TankWelder"),-18} {OnOff(c.TankWelderEnabled)} (T1 {c.TankWelderT1Multiplier:0.0}x / T2 {c.TankWelderT2Multiplier:0.0}x / T3 {c.TankWelderT3Multiplier:0.0}x)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.BatteryRework"),-18} {OnOff(c.BatteryReworkEnabled)} (RB{c.ReinforcedBatteryCapacity}/RPC{c.ReinforcedPowerCellCapacity}/HB{c.HyperBatteryCapacity}/HPC{c.HyperPowerCellCapacity})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.TeleportBeacon"),-18} {OnOff(c.TeleportBeaconEnabled)} ({c.TeleportSourceCostJoules}+{c.TeleportTargetCostJoules} J, cd {c.TeleportCooldownSeconds}s, min {c.TeleportMinBasePowerPercent}%)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.LockerMover"),-18} {OnOff(c.LockerMoverEnabled)} (key {c.LockerMoverKey}, clipboard: {(InferiusQoL.Features.LockerMover.LockerMoverClipboard.HasContent ? $"{InferiusQoL.Features.LockerMover.LockerMoverClipboard.ItemCount} items ({InferiusQoL.Features.LockerMover.LockerMoverClipboard.SourceTechType})" : "empty")})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.AutoCraft"),-18} {OnOff(c.AutoCraftEnabled)} (storage: {c.AutoCraftUseStorage}, return: {c.AutoCraftReturnSurplus}, tooltips: {OnOff(c.AutoCraftBetterTooltips)})");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.InventoryViewer"),-18} {OnOff(c.InventoryViewerEnabled)} (key {c.InventoryViewerKey}, range {c.InventoryViewerRangeMeters}m)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.OxygenRefill"),-18} {OnOff(c.OxygenRefillEnabled)} ({c.OxygenRefillRate} units/s)");
        sb.AppendLine($"  {L.Get("InferiusQoL.Status.ScannerRoom"),-18} {OnOff(c.ScannerRoomDrillableScanEnabled)} (drillable deposits, time capsules)");
        return sb.ToString();
    }

    [ConsoleCommand("qol_log_level")]
    public static string QolLogLevel(string level = "")
    {
        if (string.IsNullOrEmpty(level))
            return L.Get("InferiusQoL.Console.LogLevelCurrent", QoLLog.CurrentVerbosity);

        QoLLog.SetVerbosity(level);
        return L.Get("InferiusQoL.Console.LogLevelSet", QoLLog.CurrentVerbosity);
    }

    [ConsoleCommand("qol_retriever_rescan")]
    public static string QolRetrieverRescan() => L.Get("InferiusQoL.Console.NotImplemented", "retriever");

    [ConsoleCommand("qol_retriever_dump")]
    public static string QolRetrieverDump() => L.Get("InferiusQoL.Console.NotImplemented", "retriever");

    [ConsoleCommand("qol_seamoth_boost_state")]
    public static string QolSeamothBoostState() => L.Get("InferiusQoL.Console.NotImplemented", "seamoth_turbo");

    [ConsoleCommand("qol_teleport_list")]
    public static string QolTeleportList() => L.Get("InferiusQoL.Console.NotImplemented", "teleport_beacon");

    /// <summary>
    /// Safely decompresses every compressed item: finds them in the player's
    /// inventory plus every StorageContainer in the scene, removes them from the
    /// container, drops them as world loot near the player, and clears the marker.
    /// On the next pickup, they are inserted at vanilla size.
    ///
    /// Use BEFORE uninstalling the mod. Otherwise vanilla Subnautica does not know
    /// about the markers, and compressed items may not fit in their original place
    /// during load.
    /// </summary>
    [ConsoleCommand("qol_compressor_decompress_all")]
    public static string QolCompressorDecompressAll()
    {
        return InferiusQoL.Features.Compressor.CompressorDecompressAll.Run();
    }

    private static string OnOff(bool v) => v ? L.Get("InferiusQoL.On") : L.Get("InferiusQoL.Off");
}
