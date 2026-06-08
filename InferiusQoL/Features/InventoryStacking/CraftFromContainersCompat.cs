#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking;

internal static class CraftFromContainersCompat
{
	private static bool _resolved;

	private static bool _present;

	private static Type _pluginType;

	private static Func<bool> _modEnabled;

	private static Func<float> _range;

	private static IDictionary<Component, ItemsContainer> _cachedContainers;

	private static void EnsureResolved()
	{
		if (_resolved)
		{
			return;
		}
		_resolved = true;
		try
		{
			foreach (PluginInfo value in Chainloader.PluginInfos.Values)
			{
				if (!((Object)(object)((value != null) ? value.Instance : null) == (Object)null))
				{
					BepInPlugin metadata = value.Metadata;
					if (!(((metadata != null) ? metadata.GUID : null) != "aedenthorn.CraftFromContainers"))
					{
						_pluginType = ((object)value.Instance).GetType();
						break;
					}
				}
			}
			if (!(_pluginType == null))
			{
				_present = true;
				_modEnabled = () => ReadConfigBool("modEnabled");
				_range = () => ReadConfigFloat("range");
				_cachedContainers = _pluginType.GetField("cachedContainers", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) as IDictionary<Component, ItemsContainer>;
			}
		}
		catch (Exception ex)
		{
			_present = false;
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				log.LogDebug((object)("CraftFromContainersCompat init: " + ex.Message));
			}
		}
	}

	public static int NearbyUnits(TechType tech)
	{
		EnsureResolved();
		if (!_present || _modEnabled == null || !_modEnabled() || _cachedContainers == null)
		{
			return 0;
		}
		Inventory main = Inventory.main;
		if (((main != null) ? main.container : null) == null || (Object)(object)Player.main == (Object)null)
		{
			return 0;
		}
		float num = ((_range != null) ? _range() : 0f);
		float num2 = num * num;
		Vector3 position = ((Component)Player.main).transform.position;
		ItemsContainer container = main.container;
		int num3 = 0;
		Component[] array = (Component[])(object)new Component[_cachedContainers.Count];
		_cachedContainers.Keys.CopyTo(array, 0);
		foreach (Component val in array)
		{
			if (!((Object)(object)val == (Object)null) && _cachedContainers.TryGetValue(val, out var value) && value != null && value != container)
			{
				Vector3 val2 = val.transform.position - position;
				if (!(val2.sqrMagnitude > num2))
				{
					num3 += CraftingCounts.UnitsIn(value, tech);
				}
			}
		}
		return num3;
	}

	private static bool ReadConfigBool(string name)
	{
		if (_pluginType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) is ConfigEntry<bool> val)
		{
			return val.Value;
		}
		return false;
	}

	private static float ReadConfigFloat(string name)
	{
		if (_pluginType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) is ConfigEntry<float> val)
		{
			return val.Value;
		}
		return 0f;
	}
}
