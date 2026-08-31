using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.ViewModels;

namespace EngineeringOnboardingApp.Views;

public partial class ValidationView : UserControl
{
    private ValidationViewModel? _vm;

    public ValidationView()
    {
        InitializeComponent();
        Loaded += ValidationView_Loaded;
    }

    private void ValidationView_Loaded(object sender, RoutedEventArgs e)
    {
        _vm = new ValidationViewModel();
        DataContext = _vm;
        ResultsGrid.ItemsSource = _vm.Results;
        _vm.PropertyChanged += Vm_PropertyChanged;
        Refresh();
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ValidationViewModel.IsRunning) ||
            e.PropertyName == nameof(ValidationViewModel.StatusMessage) ||
            e.PropertyName == nameof(ValidationViewModel.OkCount) ||
            e.PropertyName == nameof(ValidationViewModel.WarnCount) ||
            e.PropertyName == nameof(ValidationViewModel.ErrorCount))
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_vm == null)
            return;

        OkBadge.Text = $"OK · {_vm.OkCount}";
        WarnBadge.Text = $"WARN · {_vm.WarnCount}";
        ErrorBadge.Text = $"ERROR · {_vm.ErrorCount}";
        StatusText.Text = _vm.StatusMessage;
        SummaryText.Text = $"{_vm.ErrorCount} error(s), {_vm.WarnCount} warning(s), {_vm.OkCount} ok";
    }
}
