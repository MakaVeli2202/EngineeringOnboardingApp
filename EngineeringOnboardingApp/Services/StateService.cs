using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class StateService
{
    private readonly string _stateDirectory;
    private readonly string _stateFile;

    public StateService()
    {
        _stateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EngineeringOnboardingApp");

        Directory.CreateDirectory(_stateDirectory);

        _stateFile = Path.Combine(_stateDirectory, "state.json");
    }

    public AppState Load()
    {
        if (!File.Exists(_stateFile))
            return new AppState();

        try
        {
            var json = File.ReadAllText(_stateFile);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Deserialize<AppState>(json, options) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(IEnumerable<OnboardingStep> steps, IEnumerable<ToolItem> tools, string role = "", bool roleProvided = false)
    {
        var state = new AppState
        {
            Steps = steps.Select(s => new StepState
            {
                Id = s.Id,
                Status = s.Status,
                IsSelected = s.IsSelected
            }).ToList(),

            Tools = tools.Select(t => new ToolState
            {
                Id = t.Id,
                Status = t.Status,
                IsSelected = t.IsSelected,
                IsInstalled = t.IsInstalled
            }).ToList(),

            Role = role,
            RoleProvided = roleProvided
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(state, options);
        File.WriteAllText(_stateFile, json);
    }

    public void Clear()
    {
        if (File.Exists(_stateFile))
            File.Delete(_stateFile);
    }
}