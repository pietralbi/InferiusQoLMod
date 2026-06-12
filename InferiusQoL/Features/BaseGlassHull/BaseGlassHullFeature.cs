namespace InferiusQoL.Features.BaseGlassHull;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InferiusQoL.Config;
using InferiusQoL.Logging;

public static class BaseGlassHullFeature
{
    private static readonly BindingFlags StaticFields =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly HashSet<TechType> TargetFaceRecipes = new HashSet<TechType>
    {
        TechType.BaseWindow,
        TechType.BaseHatch,
        TechType.BaseGlassDome,
        TechType.BaseLargeGlassDome,
    };

    private static readonly Base.CellType[] TargetCellTypes =
    {
        Base.CellType.Room,
        Base.CellType.Corridor,
        Base.CellType.Observatory,
        Base.CellType.Moonpool,
        Base.CellType.MoonpoolRotated,
        Base.CellType.MapRoom,
        Base.CellType.MapRoomRotated,
        Base.CellType.LargeRoom,
        Base.CellType.LargeRoomRotated,
    };

    private static readonly Dictionary<int, float> VanillaFaceHullStrength = new Dictionary<int, float>();
    private static readonly Dictionary<int, float> VanillaCellHullStrength = new Dictionary<int, float>();
    private static readonly Dictionary<string, float[]> VanillaMoonpoolArrays = new Dictionary<string, float[]>();

    private static bool capturedVanillaValues;
    private static bool warnedMissingArrays;

    public static void Init()
    {
        ApplyRuntime(InferiusConfig.Instance);
    }

    public static void ApplyRuntime(InferiusConfig? cfg = null)
    {
        cfg ??= InferiusConfig.Instance;

        if (!TryGetBaseArrays(out var faceToRecipe, out var faceHullStrength, out var cellHullStrength))
        {
            if (!warnedMissingArrays)
            {
                warnedMissingArrays = true;
                QoLLog.Warning(Category.BaseGlass, "Could not find Base hull-strength arrays; glass hull multiplier was not applied.");
            }
            return;
        }

        EnsureCaptured(faceToRecipe, faceHullStrength, cellHullStrength);

        var multiplier = Clamp01(cfg.BaseGlassHullPenaltyMultiplier);
        var changed = 0;

        foreach (var pair in VanillaFaceHullStrength)
        {
            if (pair.Key < 0 || pair.Key >= faceHullStrength.Length) continue;

            var scaled = ScalePenalty(pair.Value, multiplier);
            if (!Approximately(faceHullStrength[pair.Key], scaled))
            {
                faceHullStrength[pair.Key] = scaled;
                changed++;
            }
        }

        foreach (var pair in VanillaCellHullStrength)
        {
            if (pair.Key < 0 || pair.Key >= cellHullStrength.Length) continue;

            var scaled = ScalePenalty(pair.Value, multiplier);
            if (!Approximately(cellHullStrength[pair.Key], scaled))
            {
                cellHullStrength[pair.Key] = scaled;
                changed++;
            }
        }

        changed += ApplyMoonpoolArrays(multiplier);

        QoLLog.Info(Category.BaseGlass,
            $"Glass hull penalty multiplier applied: {multiplier:0.##}x ({changed} values changed)");
    }

    private static void EnsureCaptured(TechType[] faceToRecipe, float[] faceHullStrength, float[] cellHullStrength)
    {
        if (capturedVanillaValues) return;

        for (var i = 0; i < faceToRecipe.Length && i < faceHullStrength.Length; i++)
        {
            if (TargetFaceRecipes.Contains(faceToRecipe[i]))
                VanillaFaceHullStrength[i] = faceHullStrength[i];
        }

        foreach (var cellType in TargetCellTypes)
        {
            var index = (int)cellType;
            if (index >= 0 && index < cellHullStrength.Length)
                VanillaCellHullStrength[index] = cellHullStrength[index];
        }

        CaptureMoonpoolArrays();
        capturedVanillaValues = true;

        QoLLog.Info(Category.BaseGlass,
            $"Captured vanilla glass hull values (faces={VanillaFaceHullStrength.Count}, cells={VanillaCellHullStrength.Count}, moonpoolArrays={VanillaMoonpoolArrays.Count})");
    }

    private static bool TryGetBaseArrays(out TechType[] faceToRecipe, out float[] faceHullStrength, out float[] cellHullStrength)
    {
        var fields = typeof(Base).GetFields(StaticFields);

        var foundFaceToRecipe = GetArray<TechType>(fields, "FaceToRecipe");
        var foundFaceHullStrength = GetArray<float>(fields, "FaceHullStrength")
            ?? FindHullStrengthArray(fields, "face");
        var foundCellHullStrength = GetArray<float>(fields, "CellHullStrength")
            ?? FindHullStrengthArray(fields, "cell");

        if (foundFaceToRecipe == null || foundFaceHullStrength == null || foundCellHullStrength == null)
        {
            faceToRecipe = Array.Empty<TechType>();
            faceHullStrength = Array.Empty<float>();
            cellHullStrength = Array.Empty<float>();
            return false;
        }

        faceToRecipe = foundFaceToRecipe;
        faceHullStrength = foundFaceHullStrength;
        cellHullStrength = foundCellHullStrength;
        return true;
    }

    private static T[]? GetArray<T>(IEnumerable<FieldInfo> fields, string name)
    {
        var field = fields.FirstOrDefault(f =>
            f.FieldType == typeof(T[]) &&
            string.Equals(f.Name, name, StringComparison.Ordinal));

        return field?.GetValue(null) as T[];
    }

    private static float[]? FindHullStrengthArray(IEnumerable<FieldInfo> fields, string kind)
    {
        return fields
            .Where(f => f.FieldType == typeof(float[]))
            .Where(f =>
            {
                var lower = f.Name.ToLowerInvariant();
                return lower.Contains("hull") && lower.Contains(kind);
            })
            .Select(f => f.GetValue(null) as float[])
            .FirstOrDefault(a => a != null);
    }

    private static void CaptureMoonpoolArrays()
    {
        foreach (var field in typeof(Base).GetFields(StaticFields))
        {
            if (field.FieldType != typeof(float[])) continue;
            if (field.Name.IndexOf("moonpool", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (!(field.GetValue(null) is float[] values)) continue;
            if (!values.Any(v => v < 0f)) continue;

            VanillaMoonpoolArrays[field.Name] = (float[])values.Clone();
        }
    }

    private static int ApplyMoonpoolArrays(float multiplier)
    {
        if (VanillaMoonpoolArrays.Count == 0) return 0;

        var changed = 0;
        foreach (var field in typeof(Base).GetFields(StaticFields))
        {
            if (!VanillaMoonpoolArrays.TryGetValue(field.Name, out var vanilla)) continue;
            if (!(field.GetValue(null) is float[] values)) continue;

            var length = Math.Min(values.Length, vanilla.Length);
            for (var i = 0; i < length; i++)
            {
                var scaled = ScalePenalty(vanilla[i], multiplier);
                if (Approximately(values[i], scaled)) continue;

                values[i] = scaled;
                changed++;
            }
        }
        return changed;
    }

    private static float ScalePenalty(float vanilla, float multiplier)
    {
        if (vanilla >= 0f) return vanilla;
        return multiplier <= 0f ? 0f : vanilla * multiplier;
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value)) return 0.5f;
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }

    private static bool Approximately(float a, float b)
    {
        return Math.Abs(a - b) < 0.0001f;
    }
}
