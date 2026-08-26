using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using EngineeringOnboardingApp.ViewModels;

namespace EngineeringOnboardingApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateWindowButton();
    }

    private void LogOutputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
            textBox.ScrollToEnd();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateWindowButton();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState =
            WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState =
                WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;

            return;
        }

        DragMove();
    }

    private void UpdateWindowButton()
    {
        if (MaximizeRestoreButton == null)
            return;

        MaximizeRestoreButton.Content =
            WindowState == WindowState.Maximized
                ? "❐"
                : "▢";
    }
}