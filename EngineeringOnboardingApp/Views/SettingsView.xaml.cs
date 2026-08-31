using System.IO;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class SettingsView : UserControl
{
    private readonly AppSession _session = AppSession.Shared;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        StatePathText.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EngineeringOnboardingApp",
            "state.json");
    }

    private void ResetProgress_Click(object sender, RoutedEventArgs e)
    {
        _session.ResetState();
        _session.Log.Append("Onboarding progress reset from Settings.");
        ResetStatus.Text = "Progress reset. Navigate to Onboarding to see cleared state.";
    }

    private void SaveProgress_Click(object sender, RoutedEventArgs e)
    {
        _session.SaveState();
        _session.Log.Append("Progress saved from Settings.");
        ResetStatus.Text = "Progress saved.";
    }

    private void OpenData_Click(object sender, RoutedEventArgs e)
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EngineeringOnboardingApp");

        Directory.CreateDirectory(dir);

        try
        {
            if (!ProcessGate.ShouldLaunch("OpenDataFolder " + dir))
                return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (StatePathText.Text.Length > 0)
        {
            try
            {
                Clipboard.SetText(StatePathText.Text);
            }
            catch { }
        }
    }
}
