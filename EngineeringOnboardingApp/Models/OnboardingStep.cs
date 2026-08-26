using EngineeringOnboardingApp.Helpers;

namespace EngineeringOnboardingApp.Models;

public enum StepStatus
{
    NotStarted,
    Running,
    Completed,
    PendingApproval,
    Failed,
    Skipped
}

public class OnboardingStep : ObservableObject
{
    private StepStatus _status = StepStatus.NotStarted;
    private bool _isSelected;
    private bool _isLocked;
    private bool _isCurrent;

    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;

    public bool Required { get; set; }
    public bool Deprecated { get; set; }
    public bool IsOptional { get; set; }

    public string ActionType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ScriptPath { get; set; } = string.Empty;
    public string ScriptArguments { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;

    public StepStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent != value)
            {
                _isCurrent = value;
                OnPropertyChanged();
            }
        }
    }
}