using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran présentant une liste de packages avec un volet de détail (master-detail) :
/// sélectionner une ligne charge son détail (<c>brew info</c>).
/// </summary>
public abstract partial class PackageListViewModel : ScreenViewModel
{
    // Séquence pour ignorer les résultats de détail obsolètes (sélection rapide).
    private int _detailToken;

    protected PackageListViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    public ObservableCollection<Package> Packages { get; } = [];

    /// <summary>Package sélectionné dans la liste (pilote le volet de détail).</summary>
    [ObservableProperty]
    public partial Package? SelectedPackage { get; set; }

    /// <summary>Détail du package sélectionné, ou <c>null</c> si aucun.</summary>
    [ObservableProperty]
    public partial PackageDetails? Details { get; set; }

    /// <summary>Chargement du détail en cours (spinner local, sans overlay global).</summary>
    [ObservableProperty]
    public partial bool IsLoadingDetails { get; set; }

    /// <summary>Remplace le contenu de la liste par <paramref name="items"/>.</summary>
    protected void Replace(IReadOnlyList<Package> items)
    {
        Packages.Clear();
        foreach (var p in items)
        {
            Packages.Add(p);
        }
    }

    /// <summary>Vide la sélection et le détail (ex. après une désinstallation).</summary>
    protected void ClearSelection()
    {
        SelectedPackage = null;
        Details = null;
    }

    partial void OnSelectedPackageChanged(Package? value) => _ = LoadDetailsAsync(value);

    private async Task LoadDetailsAsync(Package? package)
    {
        var token = ++_detailToken;
        if (package is null)
        {
            Details = null;
            return;
        }

        IsLoadingDetails = true;
        try
        {
            var details = await Homebrew.GetInfoAsync(package.Name);
            if (token == _detailToken)
            {
                Details = details;
            }
        }
        catch (Exception)
        {
            if (token == _detailToken)
            {
                Details = null;
            }
        }
        finally
        {
            if (token == _detailToken)
            {
                IsLoadingDetails = false;
            }
        }
    }
}
