using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.ViewModels;

namespace EngineeringOnboardingApp.Views;

public partial class ToolsView : UserControl
{
    private ToolsViewModel? _vm;

    public ToolsView()
    {
        InitializeComponent();
        Loaded += ToolsView_Loaded;
    }

    private void ToolsView_Loaded(object sender, RoutedEventArgs e)
    {
        _vm = new ToolsViewModel();
        DataContext = _vm;
        ToolsList.ItemsSource = _vm.Tools;
        _vm.PropertyChanged += Vm_PropertyChanged;
        Refresh();
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToolsViewModel.IsRunning) ||
            e.PropertyName == nameof(ToolsViewModel.StatusMessage) ||
            e.PropertyName == nameof(ToolsViewModel.InstalledCount))
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (_vm == null)
            return;

        SummaryText.Text = $"{_vm.InstalledCount} of {_vm.TotalCount} tools installed";
        StatusBar.Text = _vm.StatusMessage;
    }
}
