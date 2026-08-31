using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class ResourcesView : UserControl
{
    public ResourcesView()
    {
        InitializeComponent();
        Loaded += ResourcesView_Loaded;
    }

    private void ResourcesView_Loaded(object sender, RoutedEventArgs e)
    {
        Build();
    }

    private void Build()
    {
        ResourcesPanel.Children.Clear();

        var resources = AppSession.Shared.Resources.ToList();
        var groups = resources
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Category) ? "General" : r.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            ResourcesPanel.Children.Add(new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(2, 6, 0, 8)
            });

            var cards = new WrapPanel();
            cards.HorizontalAlignment = HorizontalAlignment.Stretch;

            foreach (var resource in group)
            {
                var card = BuildCard(resource);
                cards.Children.Add(card);
            }

            ResourcesPanel.Children.Add(cards);
        }

        if (resources.Count == 0)
        {
            ResourcesPanel.Children.Add(new TextBlock
            {
                Text = "No resources configured. Add entries to resources.json.",
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 13,
                Margin = new Thickness(2, 10, 0, 0)
            });
        }
    }

    private static Button BuildCard(ResourceLink resource)
    {
        var title = new TextBlock
        {
            Text = resource.Name,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.Resources["TextBrush"],
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };

        var description = new TextBlock
        {
            Text = resource.Description,
            Foreground = (Brush)Application.Current.Resources["TextMutedBrush"],
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };

        var stack = new StackPanel();
        stack.Children.Add(title);
        stack.Children.Add(description);

        var button = new Button
        {
            Content = stack,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 320,
            Margin = new Thickness(0, 0, 14, 14),
            Style = (Style)Application.Current.Resources["FeatureCardStyle"],
            Cursor = Cursors.Hand
        };

        string url = resource.Url;
        button.Click += (_, _) => OpenUrl(url);
        button.ToolTip = resource.Description;
        return button;
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            if (!ProcessGate.ShouldLaunch("OpenUrl " + url))
                return;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }

    private void OpenAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var resource in AppSession.Shared.Resources)
        {
            if (!string.IsNullOrWhiteSpace(resource.Url))
                OpenUrl(resource.Url);
        }
    }
}
