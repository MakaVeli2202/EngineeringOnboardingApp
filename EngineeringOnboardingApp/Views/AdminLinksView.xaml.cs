using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EngineeringOnboardingApp.Models;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp.Views;

public partial class AdminLinksView : UserControl
{
    private readonly ConfigService _config = new();
    private ResourceLink? _editing;

    public AdminLinksView()
    {
        InitializeComponent();
        Loaded += AdminLinksView_Loaded;
    }

    private void AdminLinksView_Loaded(object sender, RoutedEventArgs e)
    {
        ShowNew();
        BuildList();
    }

    private void BuildList()
    {
        LinkList.Items.Clear();
        var resources = AppSession.Shared.Resources.ToList();
        var groups = resources
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Category) ? "General" : r.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var headerItem = new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = group.Key.ToUpperInvariant(),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x8A, 0xA5)),
                    Margin = new Thickness(2, 6, 0, 4)
                },
                IsHitTestVisible = false,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            LinkList.Items.Add(headerItem);

            foreach (var resource in group.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            {
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = resource.Name,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF4)),
                    Margin = new Thickness(2, 0, 0, 0)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(resource.Category) ? "General" : resource.Category,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x8A, 0xA5)),
                    Margin = new Thickness(2, 2, 0, 0)
                });

                var item = new ListBoxItem
                {
                    Content = sp,
                    Tag = resource,
                    Padding = new Thickness(4, 8, 4, 8),
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(2, 1, 2, 1)
                };
                LinkList.Items.Add(item);
            }
        }

        CountText.Text = $"{resources.Count} total";

        if (resources.Count == 0)
        {
            LinkList.Items.Add(new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = "No resources yet. Click \"+ New Link\" to create one.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x8A, 0xA5)),
                    FontSize = 12,
                    Margin = new Thickness(2, 8, 0, 0)
                },
                IsHitTestVisible = false,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            });
        }
    }

    private void LinkList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LinkList.SelectedItem is ListBoxItem { Tag: ResourceLink resource })
            Row_Click(resource);
    }

    private void Row_Click(ResourceLink resource)
    {
        _editing = resource;
        EditorTitle.Text = "Edit resource";
        NameBox.Text = resource.Name;
        UrlBox.Text = resource.Url;
        CategoryBox.Text = resource.Category;
        DescriptionBox.Text = resource.Description;
        BookmarkCheck.IsChecked = resource.AddToBookmarks;
        DeleteBtn.IsEnabled = true;
        DeleteBtn.Content = "Delete";
        StatusText.Visibility = Visibility.Collapsed;
        NameBox.Focus();
    }

    private void ShowNew()
    {
        _editing = null;
        EditorTitle.Text = "New resource";
        NameBox.Text = string.Empty;
        UrlBox.Text = string.Empty;
        CategoryBox.Text = string.Empty;
        DescriptionBox.Text = string.Empty;
        BookmarkCheck.IsChecked = false;
        DeleteBtn.IsEnabled = false;
        DeleteBtn.Content = "Delete";
        StatusText.Visibility = Visibility.Collapsed;
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => ShowNew();

    private void New_Click(object sender, RoutedEventArgs e)
    {
        ShowNew();
        NameBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var url = UrlBox.Text.Trim();
        var category = CategoryBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowStatus("Name is required.", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            ShowStatus("URL is required.", false);
            return;
        }

        var resources = AppSession.Shared.Resources.ToList();

        if (_editing != null)
        {
            _editing.Name = name;
            _editing.Url = url;
            _editing.Category = category;
            _editing.Description = DescriptionBox.Text.Trim();
            _editing.AddToBookmarks = BookmarkCheck.IsChecked == true;
        }
        else
        {
            resources.Add(new ResourceLink
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Url = url,
                Category = category,
                Description = DescriptionBox.Text.Trim(),
                AddToBookmarks = BookmarkCheck.IsChecked == true
            });
        }

        if (_config.SaveResources(resources))
        {
            AppSession.Shared.ReloadResources();
            _editing = AppSession.Shared.Resources.FirstOrDefault(r => r.Name == name);
            ShowStatus("Saved to resources.json.", true);
            BuildList();
        }
        else
        {
            ShowStatus("Failed to save. The file may be read-only.", false);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_editing == null)
            return;

        if (DeleteBtn.Content as string != "Confirm Delete?")
        {
            DeleteBtn.Content = "Confirm Delete?";
            ShowStatus($"Click \"{DeleteBtn.Content}\" again to permanently delete \"{_editing.Name}\".", false);
            return;
        }

        DeleteBtn.Content = "Delete";

        var resources = AppSession.Shared.Resources.Where(r => r.Id != _editing.Id).ToList();

        if (_config.SaveResources(resources))
        {
            AppSession.Shared.ReloadResources();
            _editing = null;
            ShowNew();
            ShowStatus("Deleted.", true);
            BuildList();
        }
        else
        {
            ShowStatus("Failed to delete. The file may be read-only.", false);
        }
    }

    private void ShowStatus(string message, bool success)
    {
        StatusText.Text = message;
        StatusText.Foreground = new SolidColorBrush(success
            ? Color.FromRgb(0x22, 0xC5, 0x5E)
            : Color.FromRgb(0xF4, 0x5B, 0x69));
        StatusText.Visibility = Visibility.Visible;
    }
}