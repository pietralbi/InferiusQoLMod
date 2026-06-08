#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

using System.IO;
using BepInEx;

internal static class InventoryStackingPaths
{
	internal const string ConfigFolderName = "InferiusQoL";

	internal static string ConfigDirectory => Path.Combine(Paths.ConfigPath, ConfigFolderName);

	internal static string StackSidecarPath => Path.Combine(ConfigDirectory, "stacks-by-uid.json");

	internal static string BackupsDirectory => Path.Combine(ConfigDirectory, "backups");

	internal static string BackupManifestPath => Path.Combine(ConfigDirectory, "backup-manifest.json");

	internal static string LegacyBackupPath => Path.Combine(ConfigDirectory, "stacks-by-uid.backup.json");
}
