using System.Diagnostics;
using System.IO;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class StepExecutorService
{
    public async Task ExecuteStepAsync(
        OnboardingStep step,
        Action<string> log,
        Action<int>? progressUpdate = null)
    {
        await ExecuteAsync(
            step.Title,
            step.ActionType,
            step.Url,
            step.ScriptPath,
            step.ScriptArguments,
            log,
            progressUpdate);
    }

    public async Task ExecuteToolAsync(
        ToolItem tool,
        Action<string> log,
        Action<int>? progressUpdate = null)
    {
        await ExecuteAsync(
            tool.Name,
            tool.ActionType,
            tool.Url,
            tool.ScriptPath,
            tool.ScriptArguments,
            log,
            progressUpdate);
    }

    private async Task ExecuteAsync(
        string title,
        string actionType,
        string url,
        string scriptPath,
        string scriptArguments,
        Action<string> log,
        Action<int>? progressUpdate)
    {
        progressUpdate?.Invoke(5);

        switch ((actionType ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "manualconfirm":
                log($"[INFO] Manually confirmed: {title}");
                return;

            case "openurl":
            case "opensettings":
                OpenUrl(url);
                log($"[INFO] Opened: {url}");
                return;

            case "runscript":
                await RunScriptAsync(scriptPath, scriptArguments, log, progressUpdate);
                progressUpdate?.Invoke(100);
                return;

            case "runscriptandopenurl":
                if (!string.IsNullOrWhiteSpace(url))
                {
                    OpenUrl(url);
                    log($"[INFO] Opened: {url}");
                }

                await RunScriptAsync(scriptPath, scriptArguments, log, progressUpdate);
                progressUpdate?.Invoke(100);
                return;

            default:
                throw new InvalidOperationException($"Unknown ActionType '{actionType}' for '{title}'.");
        }
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("URL is empty.");

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static async Task RunScriptAsync(
        string relativeScriptPath,
        string scriptArguments,
        Action<string> log,
        Action<int>? progressUpdate)
    {
        if (string.IsNullOrWhiteSpace(relativeScriptPath))
            throw new InvalidOperationException("Script path is empty.");

        var fullScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeScriptPath);

        if (!File.Exists(fullScriptPath))
            throw new FileNotFoundException($"Script not found: {fullScriptPath}");

        var arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{fullScriptPath}\"";

        if (!string.IsNullOrWhiteSpace(scriptArguments))
            arguments += $" {scriptArguments}";

        progressUpdate?.Invoke(15);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(fullScriptPath)!
        };

        using var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                log($"[OUT] {e.Data}");
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                log($"[ERR] {e.Data}");
        };

        log($"[INFO] Running script: {relativeScriptPath} {scriptArguments}");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        progressUpdate?.Invoke(50);

        await process.WaitForExitAsync();

        progressUpdate?.Invoke(90);

        if (process.ExitCode != 0)
            throw new Exception($"Script exited with code {process.ExitCode}");

        log($"[INFO] Script finished successfully: {relativeScriptPath}");
    }
}