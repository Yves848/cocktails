using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Cocktails.ViewModels;

namespace Cocktails.Views;

public partial class MaintenanceView : UserControl
{
    public MaintenanceView()
    {
        InitializeComponent();
        ExportBrewfileButton.Click += OnExportClick;
        ImportBrewfileButton.Click += OnImportClick;
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MaintenanceViewModel vm)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        var file = await top!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exporter le Brewfile",
            SuggestedFileName = "Brewfile",
            ShowOverwritePrompt = true,
        });

        if (file?.TryGetLocalPath() is { } path)
        {
            await vm.ExportBrewfileAsync(path);
        }
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MaintenanceViewModel vm)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        var files = await top!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choisir un Brewfile",
            AllowMultiple = false,
        });

        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            vm.ImportBrewfile(path);
        }
    }
}
