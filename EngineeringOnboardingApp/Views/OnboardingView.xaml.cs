using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.ViewModels;

namespace EngineeringOnboardingApp.Views;

public partial class OnboardingView : UserControl
{
    private OnboardingViewModel? _vm;

    public OnboardingView()
    {
        InitializeComponent();
        Loaded += OnboardingView_Loaded;
    }

    private void OnboardingView_Loaded(object sender, RoutedEventArgs e)
    {
        _vm = new OnboardingViewModel();
        DataContext = _vm;
        StepsList.ItemsSource = _vm.Steps;
        _vm.PropertyChanged += Vm_PropertyChanged;
        _vm.SelectedStep = _vm.Steps.FirstOrDefault();
        Refresh();
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnboardingViewModel.SelectedStep))
            Refresh();
    }

    private void Refresh()
    {
        if (_vm == null)
            return;

        DetailTitle.Text = _vm.SelectedStep?.Title ?? "Select a step to view details";
        DetailDescription.Text = _vm.SelectedStep?.Description ?? string.Empty;
        DetailHelp.Text = string.IsNullOrWhiteSpace(_vm.SelectedStep?.HelpText)
            ? _vm.SelectedStep?.Description ?? "Select a step to see the guide."
            : _vm.SelectedStep.HelpText;

        StepsSummary.Text = $"{_vm.StepsCompleted} completed · {_vm.StepsRemaining} remaining";
        OverallBar.Value = _vm.Steps.Count == 0 ? 0 : (double)_vm.StepsCompleted / _vm.Steps.Count * 100;

        StatusText.Text = _vm.StatusMessage;
        ProcessBar.Visibility = _vm.IsRunning ? Visibility.Visible : Visibility.Collapsed;
        RunStepButton.Content = _vm.IsOpenUrlStep ? "Run Step" : "Run Step";
    }
}
