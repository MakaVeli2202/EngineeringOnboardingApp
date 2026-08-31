using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EngineeringOnboardingApp.Helpers;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.ViewModels;

public class ValidationViewModel : BaseViewModel
{
    private readonly AppSession _session = AppSession.Shared;
    private readonly PreflightService _preflight = new();

    private bool _isRunning;
    private string _statusMessage = "Run validation to check the environment.";

    public ValidationViewModel()
    {
    }

    public ObservableCollection<PreflightItem> Results { get; } = new();

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int OkCount => Results.Count(r => r.Status == "OK");
    public int WarnCount => Results.Count(r => r.Status == "WARN");
    public int ErrorCount => Results.Count(r => r.Status == "ERROR");

    public ICommand RunCommand => new AsyncRelayCommand(_ => RunAsync(), _ => !_isRunning);
    public ICommand ClearCommand => new RelayCommand(_ => Clear());

    private async Task RunAsync()
    {
        IsRunning = true;
        StatusMessage = "Running validation checks...";
        _session.Log.Append("Starting preflight validation.");

        try
        {
            var results = await _preflight.RunAsync();

            Results.Clear();
            foreach (var item in results)
                Results.Add(item);

            StatusMessage = $"Validation complete: {ErrorCount} error(s), {WarnCount} warning(s).";
            _session.Log.Append($"Validation complete: {ErrorCount} error(s), {WarnCount} warning(s).");

            OnPropertyChanged(nameof(OkCount));
            OnPropertyChanged(nameof(WarnCount));
            OnPropertyChanged(nameof(ErrorCount));
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void Clear()
    {
        Results.Clear();
        StatusMessage = "Results cleared.";
        OnPropertyChanged(nameof(OkCount));
        OnPropertyChanged(nameof(WarnCount));
        OnPropertyChanged(nameof(ErrorCount));
    }
}
