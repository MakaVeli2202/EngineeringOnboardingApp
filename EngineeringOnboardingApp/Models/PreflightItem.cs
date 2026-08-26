using EngineeringOnboardingApp.Helpers;

namespace EngineeringOnboardingApp.Models;

public class PreflightItem : ObservableObject
{
    public string Area { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}