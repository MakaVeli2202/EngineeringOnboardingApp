using System.Diagnostics;
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
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = $"\"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
                return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
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
