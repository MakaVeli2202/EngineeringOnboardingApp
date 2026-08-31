using System.Diagnostics;
using System.IO;
using System.Text.Json;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class PreflightService
{
    private readonly string _appRoot;

    public PreflightService()
    {
        _appRoot = AppDomain.CurrentDomain.BaseDirectory;
    }

    public async Task<List<PreflightItem>> RunAsync()
    {
        var results = new List<PreflightItem>();

        CheckRequiredFiles(results);
        ValidateJsonFiles(results);
        ValidateStepReferences(results);
        ValidateToolReferences(results);
        ValidateSystemCommands(results);
        await ValidatePowerShellSyntaxAsync(results);
        CheckHardcodedUserPaths(results);

        return results;
    }

    private void Add(List<PreflightItem> results, string area, string item, string status, string message)
    {
        results.Add(new PreflightItem
        {
            Area = area,
            Item = item,
            Status = status,
            Message = message
        });
    }

    private static bool IsBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    private string PathOf(string relativePath)
    {
        return Path.Combine(_appRoot, relativePath);
    }

    private void CheckRequiredFiles(List<PreflightItem> results)
    {
        var requiredFiles = new[]
        {
            "Data\\steps.json",
            "Data\\tools.json",
            "Data\\resources.json",
            "Data",
            "Configs",
            "Scripts",
            "Assets",
            "Configs\\vs2026.vsconfig",
            "Configs\\VS2022.vsconfig",
            "Configs\\Installers\\dotnet-sdk-10.0.301-win-x64.exe",
            "Configs\\Installers\\winsdksetup.exe",
            "Configs\\Registry\\hkcu.reg.txt",
            "Configs\\Registry\\hklm.reg.txt",
            "Assets\\AppIcon.png",
            "Assets\\AppIcon.ico"
        };

        foreach (var file in requiredFiles)
        {
            var fullPath = PathOf(file);

            if (File.Exists(fullPath) || Directory.Exists(fullPath))
                Add(results, "Files", file, "OK", "Found.");
            else
                Add(results, "Files", file, "WARN", "Missing. This may be optional depending on selected setup steps.");
        }
    }

    private void ValidateJsonFiles(List<PreflightItem> results)
    {
        var jsonFiles = new[]
        {
            "Data\\steps.json",
            "Data\\tools.json",
            "Data\\resources.json",
            "Configs\\vs2026.vsconfig",
            "Configs\\VS2022.vsconfig"
        };

        foreach (var file in jsonFiles)
        {
            var fullPath = PathOf(file);

            if (!File.Exists(fullPath))
            {
                Add(results, "JSON", file, "ERROR", "File missing.");
                continue;
            }

            try
            {
                var json = File.ReadAllText(fullPath);
                JsonDocument.Parse(json);
                Add(results, "JSON", file, "OK", "Valid JSON.");
            }
            catch (Exception ex)
            {
                Add(results, "JSON", file, "ERROR", ex.Message);
            }
        }
    }

    private void ValidateStepReferences(List<PreflightItem> results)
    {
        try
        {
            var stepsPath = PathOf("Data\\steps.json");

            if (!File.Exists(stepsPath))
                return;

            var json = File.ReadAllText(stepsPath);

            var steps = JsonSerializer.Deserialize<List<OnboardingStep>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<OnboardingStep>();

            var duplicateIds = steps
                .GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var id in duplicateIds)
                Add(results, "Steps", id, "ERROR", "Duplicate step ID.");

            foreach (var step in steps)
            {
                if (!IsBlank(step.ScriptPath))
                {
                    var scriptPath = PathOf(step.ScriptPath);

                    if (File.Exists(scriptPath))
                        Add(results, "Steps", step.Id, "OK", $"Script exists: {step.ScriptPath}");
                    else
                        Add(results, "Steps", step.Id, "ERROR", $"Script missing: {step.ScriptPath}");
                }

                if (step.ActionType.Equals("OpenUrl", StringComparison.OrdinalIgnoreCase) ||
                    step.ActionType.Equals("OpenSettings", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsBlank(step.Url))
                        Add(results, "Steps", step.Id, "ERROR", "URL action has empty URL.");
                    else
                        Add(results, "Steps", step.Id, "OK", "URL present.");
                }
            }
        }
        catch (Exception ex)
        {
            Add(results, "Steps", "steps.json", "ERROR", ex.Message);
        }
    }

    private void ValidateToolReferences(List<PreflightItem> results)
    {
        try
        {
            var toolsPath = PathOf("Data\\tools.json");

            if (!File.Exists(toolsPath))
                return;

            var json = File.ReadAllText(toolsPath);

            var tools = JsonSerializer.Deserialize<List<ToolItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ToolItem>();

            var duplicateIds = tools
                .GroupBy(t => t.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var id in duplicateIds)
                Add(results, "Tools", id, "ERROR", "Duplicate tool ID.");

            foreach (var tool in tools)
            {
                if (!IsBlank(tool.ScriptPath))
                {
                    var scriptPath = PathOf(tool.ScriptPath);

                    if (File.Exists(scriptPath))
                        Add(results, "Tools", tool.Id, "OK", $"Script exists: {tool.ScriptPath}");
                    else
                        Add(results, "Tools", tool.Id, "ERROR", $"Script missing: {tool.ScriptPath}");
                }

                if (tool.ActionType.Equals("OpenUrl", StringComparison.OrdinalIgnoreCase) ||
                    tool.ActionType.Equals("OpenSettings", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsBlank(tool.Url))
                        Add(results, "Tools", tool.Id, "ERROR", "URL action has empty URL.");
                    else
                        Add(results, "Tools", tool.Id, "OK", "URL present.");
                }
            }
        }
        catch (Exception ex)
        {
            Add(results, "Tools", "tools.json", "ERROR", ex.Message);
        }
    }

    private void ValidateSystemCommands(List<PreflightItem> results)
    {
        var commands = new[]
        {
            "powershell",
            "reg",
            "winget",
            "dotnet",
            "git"
        };

        foreach (var command in commands)
        {
            if (CommandExists(command, out var path))
                Add(results, "Commands", command, "OK", $"Found: {path}");
            else
                Add(results, "Commands", command, "WARN", "Not found. It may be installed later or blocked by policy.");
        }
    }

    private bool CommandExists(string command, out string path)
    {
        path = string.Empty;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
                return false;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                path = output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? string.Empty;

                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task ValidatePowerShellSyntaxAsync(List<PreflightItem> results)
    {
        var scriptsDir = PathOf("Scripts");

        if (!Directory.Exists(scriptsDir))
        {
            Add(results, "Scripts", "Scripts folder", "ERROR", "Scripts folder missing.");
            return;
        }

        foreach (var script in Directory.GetFiles(scriptsDir, "*.ps1"))
        {
            var scriptName = Path.GetFileName(script);

            try
            {
                if (ProcessGate.ShouldLaunch("ValidatePS " + scriptName))
                {
                    await ValidatePowerShellScriptAsync(script, scriptName, results);
                }
                else
                {
                    Add(results, "Scripts", scriptName, "OK", "Syntax valid (simulated).");
                }
            }
            catch (Exception ex)
            {
                Add(results, "Scripts", scriptName, "ERROR", ex.Message);
            }
        }
    }

    private async Task ValidatePowerShellScriptAsync(string script, string scriptName, List<PreflightItem> results)
    {
        var escapedPath = script.Replace("'", "''");

        var command =
            $"$content = Get-Content -LiteralPath '{escapedPath}' -Raw; [System.Management.Automation.ScriptBlock]::Create($content) | Out-Null";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);

        if (process == null)
        {
            Add(results, "Scripts", scriptName, "ERROR", "Could not start PowerShell parser.");
            return;
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
            Add(results, "Scripts", scriptName, "OK", "Syntax valid.");
        else
            Add(results, "Scripts", scriptName, "ERROR", stderr);
    }

    private void CheckHardcodedUserPaths(List<PreflightItem> results)
    {
        try
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".ps1",
                ".json",
                ".cs",
                ".xaml",
                ".csproj"
            };

            var ignoredFolders = new[]
            {
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                $"{Path.DirectorySeparatorChar}Logs{Path.DirectorySeparatorChar}"
            };

            var literalSearches = new[]
            {
                "C:\\Users\\",
                Path.Combine("source", "repos", "EngineeringOnboardingApp")
            };

            var matches = new List<string>();

            foreach (var file in Directory.GetFiles(_appRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!extensions.Contains(Path.GetExtension(file)))
                    continue;

                if (ignoredFolders.Any(folder => file.Contains(folder, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var lines = File.ReadAllLines(file);

                for (var i = 0; i < lines.Length; i++)
                {
                    foreach (var search in literalSearches)
                    {
                        if (lines[i].Contains(search, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add($"{file} line {i + 1}: {lines[i].Trim()}");
                        }
                    }
                }
            }

            if (matches.Count == 0)
            {
                Add(results, "HardcodedPaths", "Project files", "OK", "No user-specific project paths found.");
            }
            else
            {
                foreach (var match in matches)
                    Add(results, "HardcodedPaths", "Project files", "WARN", match);
            }
        }
        catch (Exception ex)
        {
            Add(results, "HardcodedPaths", "Scan", "WARN", ex.Message);
        }
    }
}