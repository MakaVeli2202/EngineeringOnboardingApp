using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        Loaded += HomeView_Loaded;
    }

    private AppSession Session => AppSession.Shared;

    private void HomeView_Loaded(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    public void Refresh()
    {
        StepsCount.Text = $"{Session.CompletedSteps}/{Session.TotalSteps}";
        ToolsCount.Text = $"{Session.InstalledTools}/{Session.TotalTools}";
        OverallBar.Value = Session.OverallProgress;
        ProgressText.Text = $"{Session.OverallProgressPercent}%";
    }

    private void Navigate(string page)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateTo(page);
    }

    private void StartOnboarding_Click(object sender, RoutedEventArgs e) => Navigate("onboarding");
    private void StartTools_Click(object sender, RoutedEventArgs e) => Navigate("tools");
    private void OpenAccess_Click(object sender, RoutedEventArgs e) => Navigate("access");
    private void RunValidation_Click(object sender, RoutedEventArgs e) => Navigate("validation");

    private void OpenVpn_Click(object sender, RoutedEventArgs e) => OpenUrl("https://vpn.gehealthcare.com");
    private void OpenGenAi_Click(object sender, RoutedEventArgs e) => OpenUrl("https://emergeu.gehealthcare.com");
    private void OpenSaviynt_Click(object sender, RoutedEventArgs e) => OpenUrl("https://saviynt.gehealthcare.com");

    private static void OpenUrl(string url)
    {
        try
        {
            if (!ProcessGate.ShouldLaunch("OpenUrl " + url))
                return;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }
}
