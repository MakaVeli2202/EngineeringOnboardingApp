using System.Collections.Generic;
using System.Linq;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class AppSession : ViewModels.BaseViewModel
{
    public static AppSession Shared { get; } = new();

    private readonly ConfigService _config = new();
    private readonly StateService _state = new();
    private readonly LogService _log = new();

    private List<OnboardingStep> _steps = new();
    private List<ToolItem> _tools = new();
    private List<ResourceLink> _resources = new();
    private double _overallProgress;

    public LogService Log => _log;

    public IReadOnlyList<OnboardingStep> Steps => _steps;

    public IReadOnlyList<ToolItem> Tools => _tools;

    public IReadOnlyList<ResourceLink> Resources => _resources;

    public OnboardingStep? CurrentStep => _steps.FirstOrDefault(s => s.IsCurrent || s.Status == Models.StepStatus.Running);

    public double OverallProgress
    {
        get => _overallProgress;
        private set
        {
            if (SetProperty(ref _overallProgress, value))
                OnPropertyChanged(nameof(OverallProgressPercent));
        }
    }

    public int OverallProgressPercent => (int)System.Math.Round(_overallProgress);

    public int CompletedSteps =>
        _steps.Count(s => s.Status == Models.StepStatus.Completed);

    public int TotalSteps => _steps.Count;

    public int InstalledTools =>
        _tools.Count(t => t.IsInstalled);

    public int TotalTools => _tools.Count;

    public void Load()
    {
        _steps = _config.LoadSteps();
        _tools = _config.LoadTools();
        _resources = _config.LoadResources();

        var saved = _state.Load();

        foreach (var savedStep in saved.Steps)
        {
            var step = _steps.FirstOrDefault(s => s.Id == savedStep.Id);
            if (step != null)
            {
                step.Status = savedStep.Status;
                step.IsSelected = savedStep.IsSelected;
            }
        }

        foreach (var savedTool in saved.Tools)
        {
            var tool = _tools.FirstOrDefault(t => t.Id == savedTool.Id);
            if (tool != null)
            {
                tool.Status = savedTool.Status;
                tool.IsSelected = savedTool.IsSelected;
                tool.IsInstalled = savedTool.IsInstalled;
            }
        }

        RecomputeProgress();
    }

    public void SaveState()
    {
        _state.Save(_steps, _tools);
        RecomputeProgress();
    }

    public void ReloadResources()
    {
        _resources = _config.LoadResources();
        OnPropertyChanged(nameof(Resources));
    }

    public void ResetState()
    {
        _state.Clear();

        foreach (var step in _steps)
        {
            step.Status = Models.StepStatus.NotStarted;
            step.IsSelected = false;
            step.IsLocked = false;
            step.IsCurrent = false;
        }

        foreach (var tool in _tools)
        {
            tool.Status = Models.StepStatus.NotStarted;
            tool.IsSelected = false;
            tool.IsInstalled = false;
        }

        RecomputeProgress();
    }

    public void RecomputeProgress()
    {
        double total = _steps.Count;
        double done = _steps.Count(s => s.Status == Models.StepStatus.Completed);

        OverallProgress = total == 0 ? 0 : (done / total) * 100.0;
    }
}
