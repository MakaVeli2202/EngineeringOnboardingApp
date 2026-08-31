using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EngineeringOnboardingApp.Services;
using EngineeringOnboardingApp.Views;

namespace EngineeringOnboardingApp
{
    public partial class MainWindow : Window
    {
        private static readonly Brush NavMuted = new SolidColorBrush(Color.FromRgb(0x8A, 0x91, 0xA3));
        private static readonly Brush NavActive = new SolidColorBrush(Color.FromRgb(0xA8, 0x55, 0xF7));

        private readonly AppSession _session = AppSession.Shared;

        public MainWindow()
        {
            InitializeComponent();

            _session.Load();
            _session.Log.Append("Application started.");

            ActivateNav(BtnHome);
            NavigateTo("home");
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _session.SaveState();
            _session.Log.Append("Application closed.");
            base.OnClosed(e);
        }

        private void ResetNavigation()
        {
            BtnHome.Tag = null;
            BtnOnboarding.Tag = null;
            BtnTools.Tag = null;
            BtnAccess.Tag = null;
            BtnResources.Tag = null;
            BtnValidation.Tag = null;
            BtnLogs.Tag = null;
            BtnSettings.Tag = null;
            BtnAdmin.Tag = null;

            SetNavColors(IconHomePath, TextHome, false);
            SetNavColors(IconOnboardingPath, TextOnboarding, false);
            SetNavColors(IconToolsPath, TextTools, false);
            SetNavColors(IconAccessPath, TextAccess, false);
            SetNavColors(IconResourcesPath, TextResources, false);
            SetNavColors(IconValidationPath, TextValidation, false);
            SetNavColors(IconLogsPath, TextLogs, false);
            SetNavColors(IconSettingsPath, TextSettings, false);
            SetNavColors(IconAdminPath, TextAdmin, false);
        }

        private void ActivateNav(Button button)
        {
            button.Tag = "active";

            if (button == BtnHome) SetNavColors(IconHomePath, TextHome, true);
            else if (button == BtnOnboarding) SetNavColors(IconOnboardingPath, TextOnboarding, true);
            else if (button == BtnTools) SetNavColors(IconToolsPath, TextTools, true);
            else if (button == BtnAccess) SetNavColors(IconAccessPath, TextAccess, true);
            else if (button == BtnResources) SetNavColors(IconResourcesPath, TextResources, true);
            else if (button == BtnValidation) SetNavColors(IconValidationPath, TextValidation, true);
            else if (button == BtnLogs) SetNavColors(IconLogsPath, TextLogs, true);
            else if (button == BtnSettings) SetNavColors(IconSettingsPath, TextSettings, true);
            else if (button == BtnAdmin) SetNavColors(IconAdminPath, TextAdmin, true);
        }

        private static void SetNavColors(Path icon, TextBlock text, bool active)
        {
            Brush color = active ? NavActive : NavMuted;
            icon.Stroke = color;
            text.Foreground = color;
        }

        public void NavigateTo(string page)
        {
            ResetNavigation();

            switch ((page ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "home":
                    ActivateNav(BtnHome);
                    PageTitle.Text = "Home";
                    var home = new HomeView();
                    MainContent.Content = home;
                    home.Refresh();
                    break;

                case "onboarding":
                    ActivateNav(BtnOnboarding);
                    PageTitle.Text = "Onboarding";
                    MainContent.Content = new OnboardingView();
                    break;

                case "tools":
                    ActivateNav(BtnTools);
                    PageTitle.Text = "Tools & Software";
                    var tools = new ToolsView();
                    MainContent.Content = tools;
                    tools.Refresh();
                    break;

                case "access":
                    ActivateNav(BtnAccess);
                    PageTitle.Text = "Access & Requests";
                    MainContent.Content = new AccessView();
                    break;

                case "resources":
                    ActivateNav(BtnResources);
                    PageTitle.Text = "Resources";
                    MainContent.Content = new ResourcesView();
                    break;

                case "validation":
                    ActivateNav(BtnValidation);
                    PageTitle.Text = "Validation";
                    MainContent.Content = new ValidationView();
                    break;

                case "logs":
                    ActivateNav(BtnLogs);
                    PageTitle.Text = "Logs";
                    MainContent.Content = new LogsView();
                    break;

                case "settings":
                    ActivateNav(BtnSettings);
                    PageTitle.Text = "Settings";
                    MainContent.Content = new SettingsView();
                    break;

                case "admin":
                    ActivateNav(BtnAdmin);
                    PageTitle.Text = "Administration";
                    MainContent.Content = new AdminView();
                    break;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnHome_Click(object sender, RoutedEventArgs e) => NavigateTo("home");
        private void BtnOnboarding_Click(object sender, RoutedEventArgs e) => NavigateTo("onboarding");
        private void BtnTools_Click(object sender, RoutedEventArgs e) => NavigateTo("tools");
        private void BtnAccess_Click(object sender, RoutedEventArgs e) => NavigateTo("access");
        private void BtnResources_Click(object sender, RoutedEventArgs e) => NavigateTo("resources");
        private void BtnValidation_Click(object sender, RoutedEventArgs e) => NavigateTo("validation");
        private void BtnLogs_Click(object sender, RoutedEventArgs e) => NavigateTo("logs");
        private void BtnSettings_Click(object sender, RoutedEventArgs e) => NavigateTo("settings");
        private void BtnAdmin_Click(object sender, RoutedEventArgs e) => NavigateTo("admin");
    }
}
