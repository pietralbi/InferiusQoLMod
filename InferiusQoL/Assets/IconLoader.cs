namespace InferiusQoL.Assets;

using System.IO;
using System.Reflection;
using InferiusQoL.Logging;
using Nautilus.Utility;
using UnityEngine;

/// <summary>
/// Loads custom PNG icons from the Assets/Icons/ folder beside the DLL. If the file
/// does not exist or cannot be loaded, returns null and the caller must use a
/// fallback, usually a vanilla sprite.
///
/// Icons are deployed by the csproj Target to BepInEx/plugins/InferiusQoL/Assets/Icons/.
/// </summary>
public static class IconLoader
{
    public static Sprite? Load(string fileName)
    {
        var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dllDir)) return null;

        var path = Path.Combine(dllDir!, "Assets", "Icons", fileName);
        if (!File.Exists(path))
        {
            QoLLog.Warning(Category.Core, $"Icon not found: {fileName} (tried {path})");
            return null;
        }

        try
        {
            var texture = ImageUtils.LoadTextureFromFile(path);
            if (texture == null)
            {
                QoLLog.Warning(Category.Core, $"Icon texture failed to load: {fileName}");
                return null;
            }
            return ImageUtils.LoadSpriteFromTexture(texture);
        }
        catch (System.Exception ex)
        {
            QoLLog.Error(Category.Core, $"Icon load exception for {fileName}", ex);
            return null;
        }
    }

    /// <summary>Loads an icon or returns a vanilla sprite as fallback.</summary>
    public static Sprite LoadOrFallback(string fileName, TechType fallback)
    {
        var s = Load(fileName);
        return s ?? SpriteManager.Get(fallback);
    }

    /// <summary>Loads a PNG as Texture2D for use in a 3D model material.</summary>
    public static Texture2D? LoadTexture(string fileName)
    {
        var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dllDir)) return null;

        var path = Path.Combine(dllDir!, "Assets", "Icons", fileName);
        if (!File.Exists(path)) return null;

        try { return ImageUtils.LoadTextureFromFile(path); }
        catch (System.Exception ex)
        {
            QoLLog.Error(Category.Core, $"Texture load exception for {fileName}", ex);
            return null;
        }
    }
}
