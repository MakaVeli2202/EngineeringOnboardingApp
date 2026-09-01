namespace EngineeringOnboardingApp.Models;

public class AppState
{
    public List<StepState> Steps { get; set; } = new();
    public List<ToolState> Tools { get; set; } = new();
    public string Role { get; set; } = string.Empty;
    public bool RoleProvided { get; set; }
}

public class StepState
{
    public string Id { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.NotStarted;
    public bool IsSelected { get; set; }
}

public class ToolState
{
    public string Id { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.NotStarted;
    public bool IsSelected { get; set; }
    public bool IsInstalled { get; set; }
}