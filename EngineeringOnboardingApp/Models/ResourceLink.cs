namespace EngineeringOnboardingApp.Models;

public class ResourceLink
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AddToBookmarks { get; set; }
}