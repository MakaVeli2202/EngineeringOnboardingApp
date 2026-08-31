using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class AccessView : UserControl
{
    public AccessView()
    {
        InitializeComponent();
    }

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

    private void OpenVpn_Click(object sender, RoutedEventArgs e) => OpenUrl("https://vpn.gehealthcare.com");
    private void OpenSso_Click(object sender, RoutedEventArgs e) => OpenUrl("https://ssoportal.gehealthcare.com");
    private void OpenE5_Click(object sender, RoutedEventArgs e) => OpenUrl("https://outlook.office.com");
    private void OpenGenAi_Click(object sender, RoutedEventArgs e) => OpenUrl("https://emergeu.gehealthcare.com");
    private void OpenSaviynt_Click(object sender, RoutedEventArgs e) => OpenUrl("https://saviynt.gehealthcare.com");
    private void OpenUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ProcessGate.ShouldLaunch("OpenSettings windows-update"))
                return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
