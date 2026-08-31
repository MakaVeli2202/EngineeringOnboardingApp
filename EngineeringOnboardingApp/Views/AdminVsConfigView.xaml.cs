using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class AdminVsConfigView : UserControl
{
    private readonly VsConfigManager _manager = new();
    private string _target = VsConfigManager.Target2022;
    private VsConfig _config = new();
    private string _status = string.Empty;

    public AdminVsConfigView()
    {
        InitializeComponent();
        Loaded += AdminVsConfigView_Loaded;
    }

    private void AdminVsConfigView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadTarget(_target);
        RefreshTargetButtons();
    }

    private void LoadTarget(string target)
    {
        _target = target;
        var state = _manager.Load(target);

        if (state.LoadError != null)
        {
            _config = new VsConfig();
            ShowStatus($"Could not read {_manager.FileNameFor(target)}: {state.LoadError.Message}", false);
        }
        else if (!_manager.Exists(target))
        {
            _config = new VsConfig();
            ShowStatus($"No {_manager.FileNameFor(target)} exists yet. Add components and press Save.", true);
        }
        else
        {
            _config = state.Config;
            if (!string.IsNullOrEmpty(_status))
                ShowStatus(_status, true);
        }

        BuildEditor();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var path = _manager.PathFor(_target);
        string modified = "not present";
        try
        {
            if (File.Exists(path))
                modified = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
        }
        catch { }

        SummaryText.Text = $"{_target} · {_config.Components.Count} component(s) · {path}";
        VersionText.Text = $"version {_config.Version} · last modified {modified}";
    }

    private void RefreshTargetButtons()
    {
        Btn2022.FontWeight = _target == VsConfigManager.Target2022 ? FontWeights.Bold : FontWeights.Normal;
        Btn2026.FontWeight = _target == VsConfigManager.Target2026 ? FontWeights.Bold : FontWeights.Normal;
    }

    private void BuildEditor()
    {
        ComponentsPanel.Children.Clear();

        foreach (var component in _config.Components)
            ComponentsPanel.Children.Add(BuildRow(component));
    }

    private Grid BuildRow(string component)
    {
        var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textBox = new TextBox
        {
            Text = component,
            FontSize = 12.5,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            VerticalContentAlignment = VerticalAlignment.Center,
            Height = 32
        };
        textBox.TextChanged += (_, _) => MarkDirty();
        Grid.SetColumn(textBox, 0);
        row.Children.Add(textBox);

        var remove = new Button
        {
            Content = "Remove",
            Style = (Style)Application.Current.Resources["DangerButtonStyle"],
            Margin = new Thickness(8, 0, 0, 0),
            MinHeight = 32,
            Padding = new Thickness(12, 4, 12, 4)
        };
        remove.Click += (_, _) => RemoveRow(row);
        Grid.SetColumn(remove, 1);
        row.Children.Add(remove);

        return row;
    }

    private void RemoveRow(Grid row)
    {
        ComponentsPanel.Children.Remove(row);
        MarkDirty();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        AddNewComponent();
    }

    private void AddBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddNewComponent();
            e.Handled = true;
        }
    }

    private void AddNewComponent()
    {
        var value = AddBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return;

        _config.Components.Add(value);
        ComponentsPanel.Children.Add(BuildRow(value));
        AddBox.Clear();
        AddBox.Focus();
        MarkDirty();
    }

    private void CollectComponents()
    {
        _config.Components = new List<string>();
        foreach (var child in ComponentsPanel.Children)
        {
            if (child is Grid row && row.Children.Count > 0 && row.Children[0] is TextBox tb)
            {
                var text = tb.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    _config.Components.Add(text);
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        CollectComponents();
        var error = _manager.Save(_target, _config);
        if (string.IsNullOrEmpty(error))
        {
            _status = $"Saved {_manager.FileNameFor(_target)}.";
            ShowStatus(_status, true);
            UpdateSummary();
        }
        else
        {
            ShowStatus($"Save failed: {error}", false);
        }
    }

    private void Target2022_Click(object sender, RoutedEventArgs e)
    {
        _status = string.Empty;
        LoadTarget(VsConfigManager.Target2022);
        RefreshTargetButtons();
    }

    private void Target2026_Click(object sender, RoutedEventArgs e)
    {
        _status = string.Empty;
        LoadTarget(VsConfigManager.Target2026);
        RefreshTargetButtons();
    }

    private void Upload_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Import .vsconfig for {_target}",
            Filter = "Visual Studio Config (*.vsconfig;*.json)|*.vsconfig;*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var text = File.ReadAllText(dialog.FileName);
            var imported = System.Text.Json.JsonSerializer.Deserialize<VsConfig>(
                text,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (imported == null)
                throw new System.InvalidOperationException("The file did not contain a valid .vsconfig document.");

            imported.Components ??= new List<string>();
            imported.Extensions ??= new List<string>();
            imported.Components = imported.Components.FindAll(c => !string.IsNullOrWhiteSpace(c));

            _config = imported;
            BuildEditor();
            UpdateSummary();
            _status = $"Imported {Path.GetFileName(dialog.FileName)}. Review then press Save Changes.";
            ShowStatus(_status, true);
        }
        catch (System.Exception ex)
        {
            ShowStatus($"Import failed: {ex.Message}", false);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        CollectComponents();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = $"Export .vsconfig for {_target}",
            Filter = "Visual Studio Config (*.vsconfig)|*.vsconfig|JSON (*.json)|*.json",
            FileName = _manager.FileNameFor(_target),
            DefaultExt = "vsconfig",
            AddExtension = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                _config,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);

            ShowStatus($"Exported to {dialog.FileName}", true);
        }
        catch (System.Exception ex)
        {
            ShowStatus($"Export failed: {ex.Message}", false);
        }
    }

    private void MarkDirty()
    {
        ShowStatus("Unsaved changes.", true);
    }

    private void ShowStatus(string message, bool success)
    {
        StatusText.Text = message;
        StatusText.Foreground = new SolidColorBrush(success
            ? Color.FromRgb(0xC0, 0x84, 0xFC)
            : Color.FromRgb(0xF4, 0x5B, 0x69));
        StatusText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}
