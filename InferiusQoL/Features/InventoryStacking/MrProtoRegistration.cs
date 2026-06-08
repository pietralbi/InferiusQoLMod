#nullable disable
using System;
using System.Reflection;
using BepInEx.Logging;

namespace InferiusQoL.Features.InventoryStacking;

internal static class MrProtoRegistration
{
	private static bool s_registered;

	internal static void TryRegister(ManualLogSource log)
	{
		if (s_registered)
		{
			return;
		}
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type type = assemblies[i].GetType("ProtoBuf.Meta.RuntimeTypeModel", throwOnError: false);
				if (type == null)
				{
					continue;
				}
				object obj = type.GetProperty("Default")?.GetValue(null);
				if (obj == null)
				{
					continue;
				}
				MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
				foreach (MethodInfo methodInfo in methods)
				{
					if (!(methodInfo.Name != "Add"))
					{
						ParameterInfo[] parameters = methodInfo.GetParameters();
						if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Type) && parameters[1].ParameterType == typeof(bool))
						{
							methodInfo.Invoke(obj, new object[2]
							{
								typeof(StackData),
								true
							});
							log.LogInfo((object)"StackData registered for protobuf save data.");
							s_registered = true;
							return;
						}
					}
				}
			}
			log.LogWarning((object)"Could not register StackData with protobuf-net (assembly/API). Stack counts may not persist until registration succeeds.");
		}
		catch (Exception ex)
		{
			log.LogWarning((object)("StackData protobuf registration failed: " + ex.Message));
		}
	}
}
