using EngineeringOnboardingApp.Helpers;

namespace EngineeringOnboardingApp.Models;

public class ToolItem : ObservableObject
{
    private bool _isSelected;
    private bool _isInstalled;
    private StepStatus _status = StepStatus.NotStarted;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public bool Required { get; set; }
    public bool OptionalSelectable { get; set; }

    public string ActionType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ScriptPath { get; set; } = string.Empty;
    public string ScriptArguments { get; set; } = string.Empty;

    public string DetectionType { get; set; } = string.Empty;
    public string DetectionValue { get; set; } = string.Empty;

    public string HelpText { get; set; } = string.Empty;

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

    public bool IsInstalled
    {
        get => _isInstalled;
        set
        {
            if (_isInstalled != value)
            {
                _isInstalled = value;
                OnPropertyChanged();
            }
        }
    }

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
}