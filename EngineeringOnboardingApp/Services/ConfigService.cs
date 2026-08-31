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
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public List<OnboardingStep> LoadSteps()
        => Load<List<OnboardingStep>>("Data\\steps.json") ?? new List<OnboardingStep>();

    public List<ToolItem> LoadTools()
        => Load<List<ToolItem>>("Data\\tools.json") ?? new List<ToolItem>();

    public List<ResourceLink> LoadResources()
        => Load<List<ResourceLink>>("Data\\resources.json") ?? new List<ResourceLink>();

    public string ResolvePath(string relativePath)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

    public bool SaveResources(IEnumerable<ResourceLink> resources)
        => Save("Data\\resources.json", resources);

    public bool SaveSteps(IEnumerable<OnboardingStep> steps)
        => Save("Data\\steps.json", steps);

    public bool SaveTools(IEnumerable<ToolItem> tools)
        => Save("Data\\tools.json", tools);

    private bool Save<T>(string relativePath, IEnumerable<T> data)
    {
        try
        {
            var path = ResolvePath(relativePath);
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(data, _options);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);

            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private T? Load<T>(string relativePath)
    {
        var path = ResolvePath(relativePath);

        if (!File.Exists(path))
            return default;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}