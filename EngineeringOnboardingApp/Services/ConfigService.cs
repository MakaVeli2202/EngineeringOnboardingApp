using System.IO;
using System.Text.Json;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class ConfigService
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public List<OnboardingStep> LoadSteps()
        => Load<List<OnboardingStep>>("Data\\steps.json") ?? new List<OnboardingStep>();

    public List<ToolItem> LoadTools()
        => Load<List<ToolItem>>("Data\\tools.json") ?? new List<ToolItem>();

    public List<ResourceLink> LoadResources()
        => Load<List<ResourceLink>>("Data\\resources.json") ?? new List<ResourceLink>();

    private T? Load<T>(string relativePath)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        if (!File.Exists(path))
            return default;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}