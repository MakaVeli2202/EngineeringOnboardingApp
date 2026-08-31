using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
        Loaded += AdminView_Loaded;
    }

    private void AdminView_Loaded(object sender, RoutedEventArgs e)
    {
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (AdminService.Shared.Authenticated)
            ShowAuthenticated();
        else
            Host.Content = new AdminLoginView(OnLoggedIn);
    }

    private void ShowAuthenticated()
    {
        Host.Content = new AdminWorkspace(OnLoggedOut);
    }

    private void OnLoggedIn()
    {
        ShowCurrent();
    }

    private void OnLoggedOut()
    {
        ShowCurrent();
    }
}
