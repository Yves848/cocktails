using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cocktails.Core.Models;

namespace Cocktails.Views;

public partial class PackageDetailView : UserControl
{
    public PackageDetailView()
    {
        InitializeComponent();
        HomepageButton.Click += OnHomepageClick;
    }

    private async void OnHomepageClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageDetails { Homepage: { } url }
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && TopLevel.GetTopLevel(this)?.Launcher is { } launcher)
        {
            await launcher.LaunchUriAsync(uri);
        }
    }
}
