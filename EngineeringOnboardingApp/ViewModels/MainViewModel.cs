using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using EngineeringOnboardingApp.Helpers;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService = new();
    private readonly StateService _stateService = new();
    private readonly DetectionService _detectionService = new();
    private readonly StepExecutorService _executorService = new();
    private readonly PreflightService _preflightService = new();
    private readonly StringBuilder _logBuilder = new();

    private OnboardingStep? _selectedStep;
    private OnboardingStep? _browsedStep;
    private ToolItem? _selectedTool;
    private ToolItem? _browsedTool;
    private ResourceLink? _selectedResource;
    private string _logText = string.Empty;
    private int _currentProgress;
    private bool _isBusy;
    private string _preflightSummary = "Startup validation has not run.";
    private int _currentStepIndex = -1;

    public ObservableCollection<OnboardingStep> Steps { get; } = new();
    public ObservableCollection<ToolItem> Tools { get; } = new();
    public ObservableCollection<ResourceLink> Resources { get; } = new();
    public ObservableCollection<PreflightItem> PreflightItems { get; } = new();
    public ObservableCollection<OnboardingStep> CompletedSteps { get; } = new();
    public ObservableCollection<OnboardingStep> PendingSteps { get; } = new();
    public ObservableCollection<OnboardingStep> FailedSteps { get; } = new();
    public ObservableCollection<ToolItem> OptionalTools { get; } = new();

    public OnboardingStep? SelectedStep
    {
        get => _selectedStep;
        set
        {
            if (value == null)
                return;

            var index = Steps.IndexOf(value);

            if (index < 0)
                return;

            if (value.IsLocked && index > CurrentStepIndex)
                return;

            _selectedStep = value;
            OnPropertyChanged();
        }
    }

    public OnboardingStep? BrowsedStep
    {
        get => _browsedStep;
        set
        {
            if (_browsedStep == value)
                return;

            _browsedStep = value;
            OnPropertyChanged();
        }
    }

    public ToolItem? SelectedTool
    {
        get => _selectedTool;
        set
        {
            _selectedTool = value;
            OnPropertyChanged();
        }
    }

    public ToolItem? BrowsedTool
    {
        get => _browsedTool;
        set
        {
            if (_browsedTool == value)
                return;

            _browsedTool = value;
            OnPropertyChanged();
        }
    }

    public ResourceLink? SelectedResource
    {
        get => _selectedResource;
        set
        {
            _selectedResource = value;
            OnPropertyChanged();
        }
    }

    public string LogText
    {
        get => _logText;
        set
        {
            _logText = value;
            OnPropertyChanged();
        }
    }

    public int CurrentProgress
    {
        get => _currentProgress;
        set
        {
            _currentProgress = value;
            OnPropertyChanged();
        }
    }

    public string PreflightSummary
    {
        get => _preflightSummary;
        set
        {
            _preflightSummary = value;
            OnPropertyChanged();
        }
    }

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set
        {
            if (_currentStepIndex == value)
                return;

            _currentStepIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentStepDisplay));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanSkipCurrent));
            OnPropertyChanged(nameof(ProgressSummary));
            RefreshWizardState();
        }
    }

    public string CurrentStepDisplay => Steps.Count == 0
        ? "STEP 0 OF 0"
        : $"STEP {Math.Min(Math.Max(CurrentStepIndex, 0) + 1, Steps.Count)} OF {Steps.Count}";

    public int CompletedStepCount => Steps.Count(step => step.Status == StepStatus.Completed);

    public int PendingStepCount => Steps.Count(step => step.Status is StepStatus.NotStarted or StepStatus.Running or StepStatus.PendingApproval);

    public int FailedStepCount => Steps.Count(step => step.Status == StepStatus.Failed);

    public int OptionalSelectedToolCount => Tools.Count(tool => tool.OptionalSelectable && tool.IsSelected);

    public int PreflightPassedCount => PreflightItems.Count(item => item.Status == "OK");

    public int PreflightWarningCount => PreflightItems.Count(item => item.Status == "WARN");

    public int PreflightErrorCount => PreflightItems.Count(item => item.Status == "ERROR");

    public bool CanGoPrevious => CurrentStepIndex > 0;

    public bool CanGoNext => CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count - 1;

    public bool CanSkipCurrent => CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count;

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();

            ExecuteStepCommand.RaiseCanExecuteChanged();
            MarkCompletedCommand.RaiseCanExecuteChanged();
            ReloadCommand.RaiseCanExecuteChanged();
            ResetStateCommand.RaiseCanExecuteChanged();
            RunAllRequiredCommand.RaiseCanExecuteChanged();
            InstallSelectedToolsCommand.RaiseCanExecuteChanged();
            InstallToolCommand.RaiseCanExecuteChanged();
            RefreshDetectionCommand.RaiseCanExecuteChanged();
            OpenResourceCommand.RaiseCanExecuteChanged();
            RunPreflightCommand.RaiseCanExecuteChanged();
            PreviousStepCommand.RaiseCanExecuteChanged();
            NextStepCommand.RaiseCanExecuteChanged();
            SkipStepCommand.RaiseCanExecuteChanged();
            CopyLogsCommand.RaiseCanExecuteChanged();
            ClearLogsCommand.RaiseCanExecuteChanged();
            SaveLogsCommand.RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand ExecuteStepCommand { get; }
    public AsyncRelayCommand MarkCompletedCommand { get; }
    public AsyncRelayCommand ReloadCommand { get; }
    public AsyncRelayCommand ResetStateCommand { get; }
    public AsyncRelayCommand RunAllRequiredCommand { get; }
    public AsyncRelayCommand InstallSelectedToolsCommand { get; }
    public AsyncRelayCommand InstallToolCommand { get; }
    public AsyncRelayCommand RefreshDetectionCommand { get; }
    public AsyncRelayCommand OpenResourceCommand { get; }
    public AsyncRelayCommand RunPreflightCommand { get; }
    public AsyncRelayCommand PreviousStepCommand { get; }
    public AsyncRelayCommand NextStepCommand { get; }
    public AsyncRelayCommand SkipStepCommand { get; }
    public AsyncRelayCommand CopyLogsCommand { get; }
    public AsyncRelayCommand ClearLogsCommand { get; }
    public AsyncRelayCommand SaveLogsCommand { get; }

    public MainViewModel()
    {
        ExecuteStepCommand = new AsyncRelayCommand(ExecuteStepAsync, _ => !IsBusy);
        MarkCompletedCommand = new AsyncRelayCommand(MarkCompletedAsync, _ => !IsBusy);
        ReloadCommand = new AsyncRelayCommand(ReloadAsync, _ => !IsBusy);
        ResetStateCommand = new AsyncRelayCommand(ResetStateAsync, _ => !IsBusy);
        RunAllRequiredCommand = new AsyncRelayCommand(RunAllRequiredAsync, _ => !IsBusy);
        InstallSelectedToolsCommand = new AsyncRelayCommand(InstallSelectedToolsAsync, _ => !IsBusy);
        InstallToolCommand = new AsyncRelayCommand(InstallToolAsync, _ => !IsBusy);
        RefreshDetectionCommand = new AsyncRelayCommand(RefreshDetectionAsync, _ => !IsBusy);
        OpenResourceCommand = new AsyncRelayCommand(OpenResourceAsync, _ => !IsBusy);
        RunPreflightCommand = new AsyncRelayCommand(RunPreflightAsync, _ => !IsBusy);
        PreviousStepCommand = new AsyncRelayCommand(PreviousStepAsync, _ => !IsBusy);
        NextStepCommand = new AsyncRelayCommand(NextStepAsync, _ => !IsBusy);
        SkipStepCommand = new AsyncRelayCommand(SkipStepAsync, _ => !IsBusy);
        CopyLogsCommand = new AsyncRelayCommand(CopyLogsAsync, _ => !IsBusy);
        ClearLogsCommand = new AsyncRelayCommand(ClearLogsAsync, _ => !IsBusy);
        SaveLogsCommand = new AsyncRelayCommand(SaveLogsAsync, _ => !IsBusy);

        LoadAll();

        _ = RunStartupPreflightAsync();
    }

    private async Task RunStartupPreflightAsync()
    {
        await Task.Delay(300);
        await RunPreflightAsync(null);
    }

    private async Task RunPreflightAsync(object? parameter)
    {
        IsBusy = true;

        try
        {
            AppendLog("[INFO] Running startup validation...");

            var results = await _preflightService.RunAsync();

            PreflightItems.Clear();

            foreach (var item in results)
                PreflightItems.Add(item);

            var okCount = results.Count(r => r.Status == "OK");
            var warnCount = results.Count(r => r.Status == "WARN");
            var errorCount = results.Count(r => r.Status == "ERROR");

            PreflightSummary = $"Startup validation complete. OK: {okCount}, Warnings: {warnCount}, Errors: {errorCount}";

            if (errorCount > 0)
                AppendLog($"[ERROR] {PreflightSummary}");
            else if (warnCount > 0)
                AppendLog($"[WARN] {PreflightSummary}");
            else
                AppendLog($"[INFO] {PreflightSummary}");

            foreach (var warning in results.Where(r => r.Status == "WARN"))
                AppendLog($"[WARN] [{warning.Area}] {warning.Item}: {warning.Message}");

            foreach (var error in results.Where(r => r.Status == "ERROR"))
                AppendLog($"[ERROR] [{error.Area}] {error.Item}: {error.Message}");

            RefreshSummaryCounts();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string ProgressSummary =>
        $"{Steps.Count(x => x.Status == StepStatus.Completed)} of {Steps.Count} steps completed";

    private void LoadAll()
    {
        Steps.Clear();
        Tools.Clear();
        Resources.Clear();

        foreach (var step in _configService.LoadSteps())
            Steps.Add(step);

        foreach (var tool in _configService.LoadTools())
            Tools.Add(tool);

        foreach (var resource in _configService.LoadResources())
            Resources.Add(resource);

        RestoreState();
        RefreshToolDetection(logResults: false);
        InitializeWizardState();

        AppendLog("[INFO] Configuration loaded.");
    }

    private void InitializeWizardState()
    {
        if (Steps.Count == 0)
        {
            CurrentStepIndex = -1;
            _selectedStep = null;
            RefreshWizardState();
            return;
        }

        var nextIndex = -1;

        for (var i = 0; i < Steps.Count; i++)
        {
            if (Steps[i].Status is not StepStatus.Completed and not StepStatus.Skipped)
            {
                nextIndex = i;
                break;
            }
        }

        CurrentStepIndex = nextIndex >= 0 && nextIndex < Steps.Count ? nextIndex : Steps.Count - 1;
        _selectedStep = Steps[CurrentStepIndex];
        OnPropertyChanged(nameof(SelectedStep));
        RefreshWizardState();
    }

    private void RestoreState()
    {
        var state = _stateService.Load();

        foreach (var savedStep in state.Steps)
        {
            var step = Steps.FirstOrDefault(s => s.Id == savedStep.Id);
            if (step == null)
                continue;

            step.Status = savedStep.Status;
            step.IsSelected = savedStep.IsSelected;
        }

        foreach (var savedTool in state.Tools)
        {
            var tool = Tools.FirstOrDefault(t => t.Id == savedTool.Id);
            if (tool == null)
                continue;

            tool.Status = savedTool.Status;
            tool.IsSelected = savedTool.IsSelected;
            tool.IsInstalled = savedTool.IsInstalled;
        }
    }

    private void RefreshSummaryCounts()
    {
        OnPropertyChanged(nameof(CurrentStepDisplay));
        OnPropertyChanged(nameof(CompletedStepCount));
        OnPropertyChanged(nameof(PendingStepCount));
        OnPropertyChanged(nameof(FailedStepCount));
        OnPropertyChanged(nameof(OptionalSelectedToolCount));
        OnPropertyChanged(nameof(PreflightPassedCount));
        OnPropertyChanged(nameof(PreflightWarningCount));
        OnPropertyChanged(nameof(PreflightErrorCount));
        OnPropertyChanged(nameof(ProgressSummary));

        CompletedSteps.Clear();
        PendingSteps.Clear();
        FailedSteps.Clear();
        OptionalTools.Clear();

        foreach (var step in Steps.Where(step => step.Status == StepStatus.Completed))
            CompletedSteps.Add(step);

        foreach (var step in Steps.Where(step => step.Status is StepStatus.NotStarted or StepStatus.Running or StepStatus.PendingApproval))
            PendingSteps.Add(step);

        foreach (var step in Steps.Where(step => step.Status == StepStatus.Failed))
            FailedSteps.Add(step);

        foreach (var tool in Tools.Where(tool => tool.OptionalSelectable))
            OptionalTools.Add(tool);
    }

    private void RefreshWizardState()
    {
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsCurrent = i == CurrentStepIndex;
            Steps[i].IsLocked = i > CurrentStepIndex;
        }

        RefreshSummaryCounts();
        ExecuteStepCommand.RaiseCanExecuteChanged();
        MarkCompletedCommand.RaiseCanExecuteChanged();
        PreviousStepCommand.RaiseCanExecuteChanged();
        NextStepCommand.RaiseCanExecuteChanged();
        SkipStepCommand.RaiseCanExecuteChanged();
    }

    private OnboardingStep? GetCurrentStep()
    {
        if (CurrentStepIndex < 0 || CurrentStepIndex >= Steps.Count)
            return null;

        return Steps[CurrentStepIndex];
    }

    private void AdvanceToNextStep()
    {
        if (Steps.Count == 0)
            return;

        var nextIndex = -1;

        for (var i = CurrentStepIndex + 1; i < Steps.Count; i++)
        {
            if (Steps[i].Status is not StepStatus.Completed and not StepStatus.Skipped)
            {
                nextIndex = i;
                break;
            }
        }

        if (nextIndex < 0 || nextIndex >= Steps.Count)
        {
            RefreshWizardState();
            return;
        }

        CurrentStepIndex = nextIndex;
        _selectedStep = Steps[CurrentStepIndex];
        OnPropertyChanged(nameof(SelectedStep));
        RefreshWizardState();
    }

    private void SaveState()
    {
        _stateService.Save(Steps, Tools);
    }

    private void RefreshToolDetection(bool logResults = true)
    {
        var detections = new List<ToolDetectionResult>();

        foreach (var tool in Tools)
        {
            var detection = _detectionService.DetectTool(tool);
            detections.Add(detection);

            tool.IsInstalled = detection.IsInstalled;
            tool.Status = tool.IsInstalled ? StepStatus.Completed : StepStatus.NotStarted;

            ApplyDetectionToMatchingSteps(tool);

            if (logResults)
            {
                var level = detection.IsInstalled ? "INFO" : "WARN";
                AppendLog($"[{level}] Detection {detection.ToolName}: {detection.Details}");
            }
        }

        if (logResults)
        {
            var installedCount = detections.Count(d => d.IsInstalled);
            var missingCount = detections.Count - installedCount;
            AppendLog($"[INFO] Detection refresh complete. Installed: {installedCount}, Missing: {missingCount}");
        }

        RefreshSummaryCounts();
    }

    private void ApplyDetectionToMatchingSteps(ToolItem tool)
    {
        if (string.IsNullOrWhiteSpace(tool.ScriptPath))
            return;

        var normalizedToolScriptPath = NormalizeRelativePath(tool.ScriptPath);

        foreach (var step in Steps.Where(step => !string.IsNullOrWhiteSpace(step.ScriptPath)))
        {
            if (!string.Equals(NormalizeRelativePath(step.ScriptPath), normalizedToolScriptPath, StringComparison.OrdinalIgnoreCase))
                continue;

            step.Status = tool.IsInstalled ? StepStatus.Completed : StepStatus.NotStarted;
        }
    }

    private static string NormalizeRelativePath(string path)
    {
        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

        return Path.GetFullPath(fullPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToUpperInvariant();
    }

    private async Task ExecuteStepAsync(object? parameter)
    {
        var step = parameter as OnboardingStep ?? SelectedStep;

        if (step == null)
            return;

        if (step.IsLocked)
        {
            AppendLog($"[WARN] {step.Title} is locked until earlier steps are completed or skipped.");
            return;
        }

        SelectedStep = step;
        IsBusy = true;
        CurrentProgress = 0;

        try
        {
            if (step.ActionType.Equals("ManualConfirm", StringComparison.OrdinalIgnoreCase))
            {
                step.Status = StepStatus.PendingApproval;
                AppendLog($"[INFO] Review the guidance for {step.Title}, then use Mark Completed when ready.");
                return;
            }

            step.Status = StepStatus.Running;

            await _executorService.ExecuteStepAsync(
                step,
                AppendLog,
                progress => CurrentProgress = progress);

            step.Status = GetPostExecutionStatus(step.ActionType);

            if (step == GetCurrentStep() && step.Status == StepStatus.Completed)
                AdvanceToNextStep();
        }
        catch (Exception ex)
        {
            step.Status = StepStatus.Failed;
            AppendLog($"[ERROR] {step.Title}: {ex.Message}");
        }
        finally
        {
            SaveState();
            RefreshWizardState();
            IsBusy = false;
        }
    }

    private Task PreviousStepAsync(object? parameter)
    {
        if (CurrentStepIndex <= 0)
            return Task.CompletedTask;

        CurrentStepIndex--;
        _selectedStep = GetCurrentStep();
        OnPropertyChanged(nameof(SelectedStep));
        AppendLog($"[INFO] Reviewing previous step: {SelectedStep?.Title}");

        return Task.CompletedTask;
    }

    private Task NextStepAsync(object? parameter)
    {
        var currentStep = GetCurrentStep();

        if (currentStep == null)
            return Task.CompletedTask;

        if (currentStep.Status is not StepStatus.Completed and not StepStatus.Skipped and not StepStatus.PendingApproval)
        {
            AppendLog($"[WARN] Complete, skip, or confirm {currentStep.Title} before moving forward.");
            return Task.CompletedTask;
        }

        AdvanceToNextStep();
        return Task.CompletedTask;
    }

    private Task SkipStepAsync(object? parameter)
    {
        var step = GetCurrentStep();

        if (step == null)
            return Task.CompletedTask;

        step.Status = StepStatus.Skipped;
        AppendLog($"[INFO] Skipped: {step.Title}");
        SaveState();
        AdvanceToNextStep();

        return Task.CompletedTask;
    }

    private async Task InstallToolAsync(object? parameter)
    {
        var tool = parameter as ToolItem ?? SelectedTool;

        if (tool == null)
            return;

        SelectedTool = tool;
        IsBusy = true;
        CurrentProgress = 0;

        try
        {
            if (IsOpenAction(tool.ActionType))
            {
                tool.Status = StepStatus.Running;

                await _executorService.ExecuteToolAsync(
                    tool,
                    AppendLog,
                    progress => CurrentProgress = progress);

                tool.Status = StepStatus.PendingApproval;
                return;
            }

            if (tool.IsInstalled)
            {
                tool.Status = StepStatus.Completed;
                AppendLog($"[INFO] {tool.Name} is already installed.");
                return;
            }

            tool.Status = StepStatus.Running;

            await _executorService.ExecuteToolAsync(
                tool,
                AppendLog,
                progress => CurrentProgress = progress);

            var detection = _detectionService.DetectTool(tool);

            tool.IsInstalled = detection.IsInstalled;
            tool.Status = tool.IsInstalled ? StepStatus.Completed : GetPostExecutionStatus(tool.ActionType);

            ApplyDetectionToMatchingSteps(tool);
            RefreshWizardState();
        }
        catch (Exception ex)
        {
            tool.Status = StepStatus.Failed;
            AppendLog($"[ERROR] {tool.Name}: {ex.Message}");
        }
        finally
        {
            SaveState();
            IsBusy = false;
        }
    }

    private async Task RunAllRequiredAsync(object? parameter)
    {
        var automatedRequiredSteps = Steps
            .Where(s =>
                s.Required &&
                !s.Deprecated &&
                s.ActionType.Equals("RunScript", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (automatedRequiredSteps.Count == 0)
        {
            AppendLog("[INFO] No automated required steps found.");
            return;
        }

        IsBusy = true;
        CurrentProgress = 0;

        try
        {
            for (var i = 0; i < automatedRequiredSteps.Count; i++)
            {
                var step = automatedRequiredSteps[i];
                SelectedStep = step;

                AppendLog($"[INFO] Running required step {i + 1}/{automatedRequiredSteps.Count}: {step.Title}");

                step.Status = StepStatus.Running;

                try
                {
                    await _executorService.ExecuteStepAsync(
                        step,
                        AppendLog,
                        progress =>
                        {
                            var start = i * 100 / automatedRequiredSteps.Count;
                            var end = (i + 1) * 100 / automatedRequiredSteps.Count;
                            CurrentProgress = start + progress * (end - start) / 100;
                        });

                    step.Status = StepStatus.Completed;
                }
                catch (Exception ex)
                {
                    step.Status = StepStatus.Failed;
                    AppendLog($"[ERROR] {step.Title}: {ex.Message}");
                }

                SaveState();
                RefreshWizardState();
            }

            CurrentProgress = 100;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InstallSelectedToolsAsync(object? parameter)
    {
        var selectedTools = Tools
            .Where(t => t.OptionalSelectable && t.IsSelected)
            .ToList();

        if (selectedTools.Count == 0)
        {
            AppendLog("[INFO] No optional tools selected.");
            return;
        }

        IsBusy = true;
        CurrentProgress = 0;

        try
        {
            for (var i = 0; i < selectedTools.Count; i++)
            {
                var tool = selectedTools[i];
                SelectedTool = tool;

                AppendLog($"[INFO] Installing selected tool {i + 1}/{selectedTools.Count}: {tool.Name}");

                await InstallToolInternalAsync(tool, progress =>
                {
                    var start = i * 100 / selectedTools.Count;
                    var end = (i + 1) * 100 / selectedTools.Count;
                    CurrentProgress = start + progress * (end - start) / 100;
                });

                SaveState();
                RefreshWizardState();
            }

            CurrentProgress = 100;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InstallToolInternalAsync(ToolItem tool, Action<int> progressUpdate)
    {
        try
        {
            if (IsOpenAction(tool.ActionType))
            {
                tool.Status = StepStatus.Running;
                await _executorService.ExecuteToolAsync(tool, AppendLog, progressUpdate);
                tool.Status = StepStatus.PendingApproval;
                return;
            }

            if (tool.IsInstalled)
            {
                tool.Status = StepStatus.Completed;
                AppendLog($"[INFO] {tool.Name} is already installed.");
                return;
            }

            tool.Status = StepStatus.Running;

            await _executorService.ExecuteToolAsync(tool, AppendLog, progressUpdate);

            var detection = _detectionService.DetectTool(tool);

            tool.IsInstalled = detection.IsInstalled;
            tool.Status = tool.IsInstalled ? StepStatus.Completed : GetPostExecutionStatus(tool.ActionType);

            ApplyDetectionToMatchingSteps(tool);
            RefreshWizardState();
        }
        catch (Exception ex)
        {
            tool.Status = StepStatus.Failed;
            AppendLog($"[ERROR] {tool.Name}: {ex.Message}");
        }
    }

    private Task RefreshDetectionAsync(object? parameter)
    {
        IsBusy = true;

        try
        {
            RefreshToolDetection();
            SaveState();
            AppendLog("[INFO] Tool detection refreshed and saved.");
            RefreshWizardState();
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }

    private async Task OpenResourceAsync(object? parameter)
    {
        var resource = parameter as ResourceLink ?? SelectedResource;

        if (resource == null)
            return;

        try
        {
            await _executorService.ExecuteStepAsync(
                new OnboardingStep
                {
                    Title = resource.Name,
                    ActionType = "OpenUrl",
                    Url = resource.Url
                },
                AppendLog,
                progress => CurrentProgress = progress);
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR] Failed to open resource '{resource.Name}': {ex.Message}");
        }
    }

    private Task MarkCompletedAsync(object? parameter)
    {
        var step = GetCurrentStep();

        if (step == null)
            return Task.CompletedTask;

        step.Status = StepStatus.Completed;
        SaveState();

        AppendLog($"[INFO] Manually marked completed: {step.Title}");
        AdvanceToNextStep();

        return Task.CompletedTask;
    }

    private Task ReloadAsync(object? parameter)
    {
        LoadAll();
        return Task.CompletedTask;
    }

    private Task ResetStateAsync(object? parameter)
    {
        foreach (var step in Steps)
        {
            step.Status = StepStatus.NotStarted;
            step.IsSelected = false;
            step.IsCurrent = false;
            step.IsLocked = false;
        }

        foreach (var tool in Tools)
        {
            tool.Status = StepStatus.NotStarted;
            tool.IsSelected = false;
            tool.IsInstalled = false;
        }

        _stateService.Clear();

        CurrentProgress = 0;
        AppendLog("[INFO] State reset.");
        InitializeWizardState();

        return Task.CompletedTask;
    }

    private async Task CopyLogsAsync(object? parameter)
    {
        if (!string.IsNullOrWhiteSpace(LogText))
            Clipboard.SetText(LogText);

        AppendLog("[INFO] Logs copied to clipboard.");
        await Task.CompletedTask;
    }

    private async Task ClearLogsAsync(object? parameter)
    {
        _logBuilder.Clear();
        LogText = string.Empty;
        await Task.CompletedTask;
    }

    private async Task SaveLogsAsync(object? parameter)
    {
        if (string.IsNullOrWhiteSpace(LogText))
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"EngineeringOnboardingApp-logs-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(dialog.FileName, LogText, Encoding.UTF8);
            AppendLog($"[INFO] Logs saved to {dialog.FileName}");
        }
    }

    private static StepStatus GetPostExecutionStatus(string actionType)
    {
        return actionType.Trim().ToLowerInvariant() switch
        {
            "manualconfirm" => StepStatus.PendingApproval,
            "openurl" => StepStatus.PendingApproval,
            "opensettings" => StepStatus.PendingApproval,
            "runscriptandopenurl" => StepStatus.PendingApproval,
            _ => StepStatus.Completed
        };
    }

    private static bool IsOpenAction(string actionType)
    {
        return actionType.Trim().Equals("OpenUrl", StringComparison.OrdinalIgnoreCase)
            || actionType.Trim().Equals("OpenSettings", StringComparison.OrdinalIgnoreCase);
    }

    private void AppendLog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _logBuilder.AppendLine($"{DateTime.Now:HH:mm:ss} {message}");
            LogText = _logBuilder.ToString();
        });
    }
}