using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EngineeringOnboardingApp.Helpers;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.ViewModels;

public class OnboardingViewModel : BaseViewModel
{
    private readonly AppSession _session = AppSession.Shared;
    private readonly StepExecutorService _executor = new();

    private OnboardingStep? _selectedStep;
    private string _statusMessage = "Select a step to begin.";
    private bool _isRunning;
    private double _processProgress;

    public OnboardingViewModel()
    {
        Refresh();
    }

    public ObservableCollection<OnboardingStep> Steps { get; } = new();

    public OnboardingStep? SelectedStep
    {
        get => _selectedStep;
        set
        {
            if (SetProperty(ref _selectedStep, value))
            {
                OnPropertyChanged(nameof(SelectedDescription));
                OnPropertyChanged(nameof(SelectedHelpText));
                OnPropertyChanged(nameof(SelectedSection));
                OnPropertyChanged(nameof(CanRunStep));
                OnPropertyChanged(nameof(IsOpenUrlStep));
                OnPropertyChanged(nameof(IsConfirmationStep));
                OnPropertyChanged(nameof(IsScriptStep));
                OnPropertyChanged(nameof(ActionButtonLabel));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string SelectedDescription => SelectedStep?.Description ?? string.Empty;
    public string SelectedSection => SelectedStep?.Section ?? string.Empty;
    public string SelectedHelpText => SelectedStep?.HelpText ?? string.Empty;
    public bool IsOpenUrlStep => SelectedStep != null && SelectedStep.ActionType.Contains("url", System.StringComparison.OrdinalIgnoreCase);
    public bool IsConfirmationStep => SelectedStep != null && SelectedStep.ActionType.Contains("manualconfirm", System.StringComparison.OrdinalIgnoreCase);
    public bool IsScriptStep => SelectedStep != null && SelectedStep.ActionType.Contains("script", System.StringComparison.OrdinalIgnoreCase);
    public bool CanRunStep => SelectedStep != null && !_isRunning;

    public string ActionButtonLabel
    {
        get
        {
            var action = (SelectedStep?.ActionType ?? string.Empty).Trim().ToLowerInvariant();
            var hasUrl = !string.IsNullOrWhiteSpace(SelectedStep?.Url);

            if (action == "opensettings")
                return "Open Settings";
            if (action == "openurl")
                return hasUrl ? "Open Link" : "Open";
            if (action == "runscript" || action == "runscriptandopenurl")
                return "Run Setup";
            return "Run Step";
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanRunStep));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool ShowRolePrompt => !_session.RoleProvided;

    public double ProcessProgress
    {
        get => _processProgress;
        private set => SetProperty(ref _processProgress, value);
    }

    public int StepsCompleted => Steps.Count(s => s.Status == StepStatus.Completed);

    public int StepsRemaining => Steps.Count(s => s.Status == StepStatus.NotStarted);

    public ICommand RunStepCommand => new AsyncRelayCommand(_ => RunStepAsync(), _ => CanRunStep);
    public ICommand RunAllCommand => new AsyncRelayCommand(_ => RunAllAsync(), _ => !_isRunning);
    public ICommand ResetCommand => new RelayCommand(_ => Reset());
    public ICommand SelectStepCommand => new RelayCommand(p => SelectStep(p as OnboardingStep));
    public ICommand MarkCompletedCommand => new RelayCommand(_ => MarkCompleted(), _ => SelectedStep != null);
    public ICommand SetRoleCommand => new RelayCommand(p => SetRole(p as string ?? string.Empty));

    private void SelectStep(OnboardingStep? step)
    {
        if (step == null)
            return;

        SelectedStep = step;
    }

    private void Refresh()
    {
        Steps.Clear();

        foreach (var step in _session.Steps)
        {
            if (!IsStepVisibleForRole(step))
                continue;

            Steps.Add(step);
        }

        OnPropertyChanged(nameof(StepsCompleted));
        OnPropertyChanged(nameof(StepsRemaining));
    }

    private bool IsStepVisibleForRole(OnboardingStep step)
    {
        if (string.IsNullOrWhiteSpace(step.Role))
            return true;

        if (!_session.RoleProvided)
            return true;

        return string.Equals(step.Role, _session.Role, StringComparison.OrdinalIgnoreCase);
    }

    private void SetRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return;

        _session.SetRole(role);
        Refresh();

        SelectedStep = Steps.FirstOrDefault();
        OnPropertyChanged(nameof(ShowRolePrompt));
        OnPropertyChanged(nameof(StepsCompleted));
        OnPropertyChanged(nameof(StepsRemaining));
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task RunStepAsync()
    {
        var step = SelectedStep;
        if (step == null || _isRunning)
            return;

        IsRunning = true;
        ProcessProgress = 0;
        StatusMessage = $"Running: {step.Title}";

        step.Status = StepStatus.Running;

        try
        {
            _session.Log.Append($"Starting step: {step.Title}");

            double progress = 0;
            await _executor.ExecuteStepAsync(step, msg => { }, p =>
            {
                progress = p;
                Application.Current?.Dispatcher.Invoke(() => ProcessProgress = p);
            });

            if (step.Status != StepStatus.PendingApproval)
                step.Status = StepStatus.Completed;

            CompleteStep();

            _session.SaveState();
            _session.Log.Append($"Completed step: {step.Title}");
            StatusMessage = $"Completed: {step.Title}";
        }
        catch (Exception ex)
        {
            step.Status = StepStatus.Failed;
            _session.Log.Append($"Failed step '{step.Title}': {ex.Message}");
            StatusMessage = $"Failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            ProcessProgress = 0;
        }
    }

    private async Task RunAllAsync()
    {
        IsRunning = true;
        try
        {
            var remaining = Steps.Where(s => s.Status != StepStatus.Completed && !s.IsLocked).ToList();
            int total = Math.Max(1, remaining.Count);
            int done = 0;

            foreach (var step in remaining)
            {
                SelectedStep = step;
                StatusMessage = $"Running: {step.Title}";
                step.Status = StepStatus.Running;
                _session.Log.Append($"Starting step: {step.Title}");

                try
                {
                    await _executor.ExecuteStepAsync(step, msg => { }, p => { });
                    if (step.Status != StepStatus.PendingApproval)
                        step.Status = StepStatus.Completed;
                    _session.Log.Append($"Completed step: {step.Title}");
                }
                catch (Exception ex)
                {
                    step.Status = StepStatus.Failed;
                    _session.Log.Append($"Failed step '{step.Title}': {ex.Message}");
                }

                done++;
                Application.Current?.Dispatcher.Invoke(() => ProcessProgress = (double)done / total * 100);
            }

            CompleteStep();
            _session.SaveState();
        }
        finally
        {
            IsRunning = false;
            ProcessProgress = 0;
            StatusMessage = "Run complete.";
        }
    }

    private void CompleteStep()
    {
        _session.RecomputeProgress();
        OnPropertyChanged(nameof(StepsCompleted));
        OnPropertyChanged(nameof(StepsRemaining));
    }

    private void MarkCompleted()
    {
        if (SelectedStep == null)
            return;

        SelectedStep.Status = StepStatus.Completed;
        _session.SaveState();
        _session.Log.Append($"Marked completed (manual): {SelectedStep.Title}");
        CompleteStep();
    }

    private void Reset()
    {
        foreach (var step in Steps)
        {
            step.Status = StepStatus.NotStarted;
            step.IsSelected = false;
            step.IsLocked = false;
            step.IsCurrent = false;
        }

        _session.ResetState();
        _session.Log.Append("Onboarding progress reset.");
        StatusMessage = "Onboarding reset.";
        OnPropertyChanged(nameof(StepsCompleted));
        OnPropertyChanged(nameof(StepsRemaining));
    }
}
