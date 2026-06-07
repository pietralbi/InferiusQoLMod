namespace InferiusQoL.Localization;

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using InferiusQoL.Logging;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Central helper for localization. All mod text is loaded from LanguageFiles/*.json.
/// Usage: L.Get("InferiusQoL.Key") or L.Get("InferiusQoL.Key", arg1, arg2).
/// </summary>
public static class L
{
    /// <summary>Prefix for all keys in our mod, preventing collisions with vanilla keys.</summary>
    public const string Prefix = "InferiusQoL.";

    /// <summary>Cache: key -> whether it is registered, for fallback detection.</summary>
    private static readonly HashSet<string> _registeredKeys = new HashSet<string>();

    /// <summary>Loads every JSON file from LanguageFiles/ beside the DLL and registers it in the Subnautica Language system.</summary>
    public static void LoadAll()
    {
        var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dllDir))
        {
            QoLLog.Error(Category.Config, "Localization: cannot determine DLL directory");
            return;
        }

        var langDir = Path.Combine(dllDir!, "LanguageFiles");
        if (!Directory.Exists(langDir))
        {
            QoLLog.Warning(Category.Config, $"Localization: folder not found: {langDir}");
            return;
        }

        foreach (var file in Directory.GetFiles(langDir, "*.json"))
        {
            var language = Path.GetFileNameWithoutExtension(file);
            try
            {
                var json = File.ReadAllText(file);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict == null) continue;

                foreach (var kvp in dict)
                {
                    Nautilus.Handlers.LanguageHandler.SetLanguageLine(kvp.Key, kvp.Value, language);
                    _registeredKeys.Add(kvp.Key);
                }

                QoLLog.Info(Category.Config, $"Loaded {dict.Count} translations for '{language}'");
            }
            catch (System.Exception ex)
            {
                QoLLog.Error(Category.Config, $"Localization: failed to load {file}", ex);
            }
        }
    }

    /// <summary>Returns localized text for a key. If the key is not registered, returns the key itself for debugging.</summary>
    public static string Get(string key)
    {
        if (Language.main == null)
        {
            // Before Language system initialization, typically in Awake of some plugins.
            return key;
        }
        var translated = Language.main.Get(key);
        // Language.Get returns the key if translation is missing. Detect that and log in debug.
        if (translated == key && !_registeredKeys.Contains(key))
            QoLLog.Debug(Category.Config, $"Missing translation key: {key}");
        return translated;
    }

    /// <summary>Returns localized text with formatting (string.Format).</summary>
    public static string Get(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch (System.FormatException)
        {
            QoLLog.Warning(Category.Config,
                $"Localization: format error for key '{key}', template: '{template}'");
            return template;
        }
    }

    /// <summary>Is the key registered in any language?</summary>
    public static bool HasKey(string key) => _registeredKeys.Contains(key);

    /// <summary>
    /// Translates a key through the Language system. If the key is not found, or
    /// Language.main is not ready yet, returns <paramref name="englishFallback"/>.
    /// Use for CraftTree tab labels, which render directly without Language lookup;
    /// we must decide the label imperatively during registration.
    /// </summary>
    public static string GetOrFallback(string key, string englishFallback)
    {
        if (Language.main == null) return englishFallback;
        var translated = Language.main.Get(key);
        if (string.IsNullOrEmpty(translated)) return englishFallback;
        if (translated == key) return englishFallback;
        return translated;
    }
}
