using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace EngineeringOnboardingApp.Services;

public class LogService : ViewModels.BaseViewModel
{
    private readonly object _lock = new();
    private readonly ObservableCollection<string> _entries = new();
    private readonly StringBuilder _buffer = new();

    public ObservableCollection<string> Entries => _entries;

    public string FullLog
    {
        get
        {
            lock (_lock)
            {
                return _buffer.ToString();
            }
        }
    }

    public void Append(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

        lock (_lock)
        {
            _buffer.AppendLine(line);
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
            _entries.Add(line);
        });
    }

    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
            _entries.Clear();
        });
    }

    public static void LogException(Exception? exception, string source)
    {
        if (exception == null)
            return;

        var message = $"[{DateTime.Now:HH:mm:ss}] [{source}] {exception.GetType().Name}: {exception.Message}";

        try
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "crash.log");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
        }
        catch { }
    }
}
