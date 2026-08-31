using System.Collections.ObjectModel;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private string _selectedSection = "Home";

    public MainViewModel()
    {
        AppVersion = typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    public string Title => "Engineering Onboarding";

    public string AppVersion { get; }

    public string SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }
}
