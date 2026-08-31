using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class LogsView : UserControl
{
    private LogService _log = AppSession.Shared.Log;

    public LogsView()
    {
        InitializeComponent();
        Loaded += LogsView_Loaded;
    }

    private void LogsView_Loaded(object sender, RoutedEventArgs e)
    {
        LogList.ItemsSource = _log.Entries;
        _log.Entries.CollectionChanged += Entries_CollectionChanged;
        ScrollToEnd();
    }

    private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (AutoScrollCheck.IsChecked == true)
            ScrollToEnd();
    }

    private void ScrollToEnd()
    {
        Dispatcher.BeginInvoke(new System.Action(() =>
        {
            LogScroll.ScrollToEnd();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _log.Clear();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        string content = _log.FullLog;
        if (string.IsNullOrWhiteSpace(content))
            return;

        try
        {
            Clipboard.SetText(content);
        }
        catch { }
    }
}
