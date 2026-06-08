#nullable disable
using System;
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

internal static class EasyCraftCompat
{
	private const string ContainersTypeName = "EasyCraft.ClosestItemContainers";

	private const string ContainersPropName = "containers";

	private static Type _containersType;

	private static PropertyInfo _containersProperty;

	internal static Type ContainersType
	{
		get
		{
			if (_containersType == null)
			{
				_containersType = AccessTools.TypeByName("EasyCraft.ClosestItemContainers");
			}
			return _containersType;
		}
	}

	internal static ItemsContainer[] GetContainers()
	{
		Type containersType = ContainersType;
		if (containersType == null)
		{
			return null;
		}
		if (_containersProperty == null)
		{
			_containersProperty = AccessTools.Property(containersType, "containers");
		}
		return _containersProperty?.GetValue(null) as ItemsContainer[];
	}
}
