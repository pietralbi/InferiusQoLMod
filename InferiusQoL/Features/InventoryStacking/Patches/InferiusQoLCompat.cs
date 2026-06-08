#nullable disable
using System;
using System.Reflection;
using HarmonyLib;

namespace InferiusQoL.Features.InventoryStacking.Patches;

internal static class InferiusQoLCompat
{
	private const string ClosestContainersTypeName = "InferiusQoL.Features.AutoCraft.ClosestItemContainers";

	private const string MainTypeName = "InferiusQoL.Features.AutoCraft.AutoCraftMain";

	private const string ContainersPropName = "Containers";

	private static Type _closestContainersType;

	private static Type _mainType;

	private static PropertyInfo _containersProperty;

	internal static Type ClosestContainersType
	{
		get
		{
			if (_closestContainersType == null)
			{
				_closestContainersType = AccessTools.TypeByName("InferiusQoL.Features.AutoCraft.ClosestItemContainers");
			}
			return _closestContainersType;
		}
	}

	internal static Type MainType
	{
		get
		{
			if (_mainType == null)
			{
				_mainType = AccessTools.TypeByName("InferiusQoL.Features.AutoCraft.AutoCraftMain");
			}
			return _mainType;
		}
	}

	internal static ItemsContainer[] GetContainers()
	{
		Type closestContainersType = ClosestContainersType;
		if (closestContainersType == null)
		{
			return null;
		}
		if (_containersProperty == null)
		{
			_containersProperty = AccessTools.Property(closestContainersType, "Containers");
		}
		return _containersProperty?.GetValue(null) as ItemsContainer[];
	}
}
