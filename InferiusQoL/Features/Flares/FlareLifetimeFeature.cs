#nullable disable
using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using InferiusQoL.Logging;
using UnityEngine;
using HostPlugin = global::InferiusQoL.Plugin;

namespace InferiusQoL.Features.Flares;

internal static class FlareLifetimeFeature
{
    private static bool s_registered;

    public static void Init()
    {
        TryRegister(HostPlugin.Logger);

        if ((UnityEngine.Object)(object)HostPlugin.Instance != (UnityEngine.Object)null)
        {
            HostPlugin.Instance.StartCoroutine(CoRetryRegister());
        }

        QoLLog.Info(Category.Core, "Flare lifetime repair active");
    }

    private static IEnumerator CoRetryRegister()
    {
        yield return new WaitForSecondsRealtime(2f);
        TryRegister(HostPlugin.Logger);
    }

    private static void TryRegister(ManualLogSource log)
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

                object model = type.GetProperty("Default")?.GetValue(null);
                if (model == null)
                {
                    continue;
                }

                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "Add")
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Type) && parameters[1].ParameterType == typeof(bool))
                    {
                        method.Invoke(model, new object[] { typeof(FlareLifetimeData), true });
                        log?.LogInfo((object)"FlareLifetimeData registered for protobuf save data.");
                        s_registered = true;
                        return;
                    }
                }
            }

            log?.LogWarning((object)"Could not register FlareLifetimeData with protobuf-net. Lit flare ages may not persist until registration succeeds.");
        }
        catch (Exception ex)
        {
            log?.LogWarning((object)("FlareLifetimeData protobuf registration failed: " + ex.Message));
        }
    }
}
