namespace InferiusQoL.Logging;

using System;
using BepInEx.Logging;

public enum Category
{
    Core,
    Config,
    Inventory,
    Locker,
    Backpack,
    Seamoth,
    Retriever,
    Compressor,
    TankWelder,
    Battery,
    Teleport,
    Oxygen,
    LockerMover,
    AutoCraft,
    ScannerRoom
}

public enum Verbosity
{
    None = 0,
    Info = 1,
    Debug = 2,
    Trace = 3
}

public static class QoLLog
{
    private static ManualLogSource? _log;
    private static Verbosity _verbosity = Verbosity.Info;

    public static Verbosity CurrentVerbosity => _verbosity;

    public static void Initialize(ManualLogSource log, string verbosity)
    {
        _log = log;
        SetVerbosity(verbosity);
    }

    public static void SetVerbosity(string verbosity)
    {
        _verbosity = Enum.TryParse<Verbosity>(verbosity, ignoreCase: true, out var v)
            ? v
            : Verbosity.Info;
        _log?.LogInfo($"[QoLLog] verbosity = {_verbosity}");
    }

    public static void SetVerbosity(Verbosity v)
    {
        _verbosity = v;
        _log?.LogInfo($"[QoLLog] verbosity = {_verbosity}");
    }

    public static void Info(Category cat, string msg)
    {
        if (_verbosity < Verbosity.Info || _log == null) return;
        _log.LogInfo(Format(cat, msg));
    }

    public static void Warning(Category cat, string msg)
    {
        if (_log == null) return;
        _log.LogWarning(Format(cat, msg));
    }

    public static void Error(Category cat, string msg)
    {
        if (_log == null) return;
        _log.LogError(Format(cat, msg));
    }

    public static void Error(Category cat, string msg, Exception ex)
    {
        if (_log == null) return;
        _log.LogError($"{Format(cat, msg)}\n{ex}");
    }

    public static void Debug(Category cat, string msg)
    {
        if (_verbosity < Verbosity.Debug || _log == null) return;
        _log.LogDebug(Format(cat, msg));
    }

    public static void Trace(Category cat, string msg)
    {
        if (_verbosity < Verbosity.Trace || _log == null) return;
        _log.LogDebug(Format(cat, "[TRACE] " + msg));
    }

    private static string Format(Category cat, string msg) => $"[{cat}] {msg}";
}
