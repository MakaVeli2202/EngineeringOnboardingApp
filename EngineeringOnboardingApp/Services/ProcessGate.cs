using System;
using System.Collections.Generic;

namespace EngineeringOnboardingApp.Services;

/// <summary>
/// Central gate for launching external processes and running scripts.
/// When <see cref="Simulate"/> is true (used by automated UI testing), external
/// launches are recorded and skipped so no real browser, explorer, or PowerShell
/// process is started. A real user session leaves this false.
/// </summary>
public static class ProcessGate
{
    private static readonly List<string> _log = new();
    private static readonly object _lock = new();

    public static bool Simulate { get; set; }

    public static void ClearLog() { lock (_lock) _log.Clear(); }

    public static IReadOnlyList<string> Log
    {
        get { lock (_lock) return new List<string>(_log); }
    }

    /// <summary>Returns true if a real external launch should proceed.</summary>
    public static bool ShouldLaunch(string description)
    {
        if (!Simulate)
            return true;

        lock (_lock) _log.Add($"[SIM] {DateTime.Now:HH:mm:ss} {description}");
        return false;
    }
}
