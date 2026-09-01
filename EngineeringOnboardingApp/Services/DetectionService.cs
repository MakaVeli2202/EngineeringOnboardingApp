using System.IO;
using Microsoft.Win32;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class DetectionService
{
    public ToolDetectionResult DetectTool(ToolItem tool)
    {
        if (string.IsNullOrWhiteSpace(tool.DetectionType) || string.IsNullOrWhiteSpace(tool.DetectionValue))
        {
            return new ToolDetectionResult(
                tool.Id,
                tool.Name,
                false,
                tool.DetectionType ?? string.Empty,
                tool.DetectionValue ?? string.Empty,
                "No detection rule configured.");
        }

        var detectionType = tool.DetectionType.Trim().ToLowerInvariant();
        var candidates = SplitCandidates(tool.DetectionValue);

        foreach (var candidate in candidates)
        {
            var expandedCandidate = Environment.ExpandEnvironmentVariables(candidate);

            switch (detectionType)
            {
                case "fileexists":
                    if (File.Exists(expandedCandidate))
                    {
                        return new ToolDetectionResult(
                            tool.Id,
                            tool.Name,
                            true,
                            tool.DetectionType,
                            tool.DetectionValue,
                            $"Found file: {expandedCandidate}");
                    }

                    if (Directory.Exists(expandedCandidate))
                    {
                        return new ToolDetectionResult(
                            tool.Id,
                            tool.Name,
                            true,
                            tool.DetectionType,
                            tool.DetectionValue,
                            $"Found directory: {expandedCandidate}");
                    }

                    break;

                case "commandexists":
                    if (CommandExists(expandedCandidate))
                    {
                        return new ToolDetectionResult(
                            tool.Id,
                            tool.Name,
                            true,
                            tool.DetectionType,
                            tool.DetectionValue,
                            $"Found command: {expandedCandidate}");
                    }

                    break;

                case "regdisplayname":
                    if (RegistryDisplayNameExists(expandedCandidate))
                    {
                        return new ToolDetectionResult(
                            tool.Id,
                            tool.Name,
                            true,
                            tool.DetectionType,
                            tool.DetectionValue,
                            $"Found matching display name: {expandedCandidate}");
                    }

                    break;
            }
        }

        return new ToolDetectionResult(
            tool.Id,
            tool.Name,
            false,
            tool.DetectionType,
            tool.DetectionValue,
            $"Checked {candidates.Count} candidate(s) using {detectionType}.");
    }

    private static List<string> SplitCandidates(string values)
    {
        var candidates = values.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return candidates.Length == 0 ? new List<string> { values } : candidates.ToList();
    }

    private bool CommandExists(string command)
    {
        // Search the PATH directories explicitly (plus System32) instead of using
        // the legacy "where" tool, which also searches the current working directory
        // and can therefore report a tool as installed when only an unrelated file or
        // a broken stub is present in the launch folder. Returns true only when an
        // actual executable matching the command exists somewhere on the search path.
        try
        {
            var extensions = new[] { ".exe", ".cmd", ".bat", ".com" }
                .Concat(Environment.GetEnvironmentVariable("PATHEXT")?.Split(';').Where(e => !string.IsNullOrWhiteSpace(e) && e[0] == '.') ?? Array.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var directory in SearchDirectories())
            {
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine(directory, command + extension);
                    if (File.Exists(candidate))
                        return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> SearchDirectories()
    {
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var expanded = Environment.ExpandEnvironmentVariables(dir.Trim().Trim('"'));
                if (!string.IsNullOrWhiteSpace(expanded))
                    dirs.Add(expanded);
            }
        }

        dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));

        return dirs;
    }

    private bool RegistryDisplayNameExists(string displayNameContains)
    {
        var roots = new[]
        {
            RegistryHive.LocalMachine,
            RegistryHive.CurrentUser
        };

        var views = new[]
        {
            RegistryView.Registry64,
            RegistryView.Registry32
        };

        foreach (var hive in roots)
        {
            foreach (var view in views)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

                    if (uninstallKey == null)
                        continue;

                    foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        using var subKey = uninstallKey.OpenSubKey(subKeyName);
                        var displayName = subKey?.GetValue("DisplayName")?.ToString();

                        // Ignore Windows internal/system-component entries (not real apps).
                        if (subKey?.GetValue("SystemComponent") is int systemComponentInt && systemComponentInt == 1)
                            continue;
                        if (subKey?.GetValue("SystemComponent") is string systemComponentStr &&
                            int.TryParse(systemComponentStr.Trim(), out var systemComponentParsed) &&
                            systemComponentParsed == 1)
                            continue;

                        if (!string.IsNullOrWhiteSpace(displayName) &&
                            displayName.Contains(displayNameContains, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Ignore registry read issues.
                }
            }
        }

        return false;
    }
}

public sealed record ToolDetectionResult(
    string ToolId,
    string ToolName,
    bool IsInstalled,
    string DetectionType,
    string DetectionValue,
    string Details);
