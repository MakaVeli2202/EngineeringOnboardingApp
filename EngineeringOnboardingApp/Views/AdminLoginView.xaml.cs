using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class AdminLoginView : UserControl
{
    private readonly Action _onSuccess;

    public AdminLoginView(Action onSuccess)
    {
        InitializeComponent();
        _onSuccess = onSuccess;
        Loaded += AdminLoginView_Loaded;
    }

    private void AdminLoginView_Loaded(object sender, RoutedEventArgs e)
    {
        if (!AdminService.Shared.HasPasscode())
        {
            HintText.Visibility = Visibility.Visible;
            HintText.Text = "First run: enter any passcode to set the admin passcode. It is stored securely on this machine only.";
        }

        PasscodeBox.Focus();
    }

    private void PasscodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Unlock_Click(sender, e);
    }

    private void ShowToggle_Click(object sender, RoutedEventArgs e)
    {
        // PasswordBox can't be bound directly; toggle via a plain TextBox mirror.
        var showing = ShowToggle.Content.ToString() == "Hide";
        if (showing)
        {
            PasscodeBox.Password = ShowBox.Text;
            ShowBox.Visibility = Visibility.Collapsed;
            PasscodeBox.Visibility = Visibility.Visible;
            ShowToggle.Content = "Show";
            PasscodeBox.Focus();
        }
        else
        {
            ShowBox.Text = PasscodeBox.Password;
            ShowBox.Visibility = Visibility.Visible;
            PasscodeBox.Visibility = Visibility.Collapsed;
            ShowToggle.Content = "Hide";
            ShowBox.Focus();
            ShowBox.CaretIndex = ShowBox.Text.Length;
        }
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        var passcode = PasscodeBox.Password;
        if (AdminService.Shared.TryLogin(passcode))
        {
            ErrorText.Visibility = Visibility.Collapsed;
            _onSuccess();
        }
        else
        {
            ErrorText.Text = "Incorrect passcode. Please try again.";
            ErrorText.Visibility = Visibility.Visible;
            PasscodeBox.Clear();
            PasscodeBox.Focus();
        }
    }
}