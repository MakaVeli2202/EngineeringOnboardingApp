using System;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class AdminWorkspace : UserControl
{
    private readonly Action _onLogout;

    public AdminWorkspace(Action onLogout)
    {
        InitializeComponent();
        _onLogout = onLogout;
        Loaded += AdminWorkspace_Loaded;
        ShowLinks();
    }

    private void AdminWorkspace_Loaded(object sender, RoutedEventArgs e)
    {
        ShowLinks();
    }

    private void ShowLinks() => Host.Content = new AdminLinksView();

    private void ShowVsConfigs() => Host.Content = new AdminVsConfigView();

    private void SetActiveTab(bool links)
    {
        TabLinks.IsEnabled = !links;
        TabVsConfig.IsEnabled = links;
    }

    private void TabLinks_Click(object sender, RoutedEventArgs e)
    {
        ShowLinks();
        SetActiveTab(true);
    }

    private void TabVsConfig_Click(object sender, RoutedEventArgs e)
    {
        ShowVsConfigs();
        SetActiveTab(false);
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        AdminService.Shared.Logout();
        _onLogout();
    }
}
