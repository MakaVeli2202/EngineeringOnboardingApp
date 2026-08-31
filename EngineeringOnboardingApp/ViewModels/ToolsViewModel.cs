using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EngineeringOnboardingApp.Helpers;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.ViewModels;

public class ToolsViewModel : BaseViewModel
{
    private readonly AppSession _session = AppSession.Shared;
    private readonly StepExecutorService _executor = new();
    private readonly DetectionService _detector = new();

    private string _statusMessage = "Select a tool to install or verify.";
    private bool _isRunning;
    private string _searchText = string.Empty;
    private string _categoryFilter = "All";

    public ToolsViewModel()
    {
        Refresh();
    }

    public ObservableCollection<ToolItem> Tools { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

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
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) ApplyFilter(); }
    }

    public string CategoryFilter
    {
        get => _categoryFilter;
        set { if (SetProperty(ref _categoryFilter, value)) ApplyFilter(); }
    }

    public int InstalledCount => Tools.Count(t => t.IsInstalled);
    public int TotalCount => Tools.Count;

    public ICommand DetectAllCommand => new AsyncRelayCommand(_ => DetectAllAsync(), _ => !_isRunning);
    public ICommand InstallSelectedCommand => new AsyncRelayCommand(_ => InstallSelectedAsync(), _ => !_isRunning);
    public ICommand InstallAllCommand => new AsyncRelayCommand(_ => InstallAllAsync(), _ => !_isRunning);

    private void Refresh()
    {
        Tools.Clear();
        Categories.Clear();

        var all = _session.Tools.ToList();

        Categories.Add("All");
        foreach (var category in all.Select(t => t.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
            Categories.Add(category);

        foreach (var tool in all)
            Tools.Add(tool);

        ApplyFilter();
        OnPropertyChanged(nameof(InstalledCount));
        OnPropertyChanged(nameof(TotalCount));
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;

        foreach (var tool in Tools)
        {
            bool matchesCategory = CategoryFilter == "All" || tool.Category == CategoryFilter;
            bool matchesSearch = query.Length == 0 ||
                tool.Name.ToLowerInvariant().Contains(query) ||
                tool.Description.ToLowerInvariant().Contains(query) ||
                tool.Category.ToLowerInvariant().Contains(query);

            tool.IsSelected = matchesCategory && matchesSearch;
        }
    }

    private async Task DetectAllAsync()
    {
        IsRunning = true;
        StatusMessage = "Detecting installed tools...";
        _session.Log.Append("Starting tool detection.");

        try
        {
            await Task.Run(() =>
            {
                foreach (var tool in Tools)
                {
                    var result = _detector.DetectTool(tool);
                    tool.IsInstalled = result.IsInstalled;
                    tool.Status = result.IsInstalled ? StepStatus.Completed : StepStatus.NotStarted;
                }
            });

            _session.SaveState();
            _session.Log.Append("Tool detection complete.");
            StatusMessage = "Detection complete.";
            OnPropertyChanged(nameof(InstalledCount));
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task InstallSelectedAsync()
    {
        IsRunning = true;
        try
        {
            var selected = Tools.Where(t => t.IsSelected && !t.IsInstalled).ToList();

            if (selected.Count == 0)
            {
                StatusMessage = "No selected tools to install.";
                return;
            }

            foreach (var tool in selected)
            {
                StatusMessage = $"Installing: {tool.Name}";
                tool.Status = StepStatus.Running;
                _session.Log.Append($"Installing tool: {tool.Name}");

                try
                {
                    await _executor.ExecuteToolAsync(tool, _session.Log.Append, p => { });
                    tool.IsInstalled = true;
                    tool.Status = StepStatus.Completed;
                    _session.Log.Append($"Installed tool: {tool.Name}");
                }
                catch (Exception ex)
                {
                    tool.Status = StepStatus.Failed;
                    _session.Log.Append($"Failed tool '{tool.Name}': {ex.Message}");
                }
            }

            _session.SaveState();
            StatusMessage = "Installation finished.";
            OnPropertyChanged(nameof(InstalledCount));
        }
        finally
        {
            IsRunning = false;
        }
    }

    private Task InstallAllAsync()
    {
        foreach (var tool in Tools)
            tool.IsSelected = true;
        return InstallSelectedAsync();
    }
}
