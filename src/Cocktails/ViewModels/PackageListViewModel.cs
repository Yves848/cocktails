using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>
/// Écran présentant une liste de packages avec un volet de détail (master-detail) :
/// sélectionner une ligne charge son détail (<c>brew info</c>). Gère aussi un filtre
/// texte et un tri alphabétique sur la liste affichée.
/// </summary>
public abstract partial class PackageListViewModel : ScreenViewModel
{
    // Jeu complet (non filtré) ; Packages est la vue filtrée+triée liée à l'UI.
    private readonly List<Package> _all = [];

    // Séquence pour ignorer les résultats de détail obsolètes (sélection rapide).
    private int _detailToken;

    protected PackageListViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Vue affichée (filtrée par <see cref="FilterText"/>, triée par nom).</summary>
    public ObservableCollection<Package> Packages { get; } = [];

    /// <summary>Filtre texte (sous-chaîne du nom, insensible à la casse).</summary>
    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    /// <summary>Filtre par type (Tout / Formulae / Casks).</summary>
    [ObservableProperty]
    public partial PackageKindFilter KindFilter { get; set; } = PackageKindFilter.All;

    public bool IsKindAll => KindFilter == PackageKindFilter.All;
    public bool IsKindFormula => KindFilter == PackageKindFilter.Formula;
    public bool IsKindCask => KindFilter == PackageKindFilter.Cask;

    [RelayCommand]
    private void SetKindFilter(PackageKindFilter kind) => KindFilter = kind;

    partial void OnKindFilterChanged(PackageKindFilter value)
    {
        OnPropertyChanged(nameof(IsKindAll));
        OnPropertyChanged(nameof(IsKindFormula));
        OnPropertyChanged(nameof(IsKindCask));
        ApplyFilter();
    }

    /// <summary>Package sélectionné dans la liste (pilote le volet de détail).</summary>
    [ObservableProperty]
    public partial Package? SelectedPackage { get; set; }

    /// <summary>Détail du package sélectionné, ou <c>null</c> si aucun.</summary>
    [ObservableProperty]
    public partial PackageDetails? Details { get; set; }

    /// <summary>Chargement du détail en cours (spinner local, sans overlay global).</summary>
    [ObservableProperty]
    public partial bool IsLoadingDetails { get; set; }

    /// <summary>Remplace le jeu complet puis applique filtre + tri.</summary>
    protected void Replace(IReadOnlyList<Package> items)
    {
        _all.Clear();
        _all.AddRange(items);
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filter = FilterText.Trim();
        IEnumerable<Package> query = _all;
        if (filter.Length > 0)
        {
            query = query.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        query = KindFilter switch
        {
            PackageKindFilter.Formula => query.Where(p => p.Kind == PackageKind.Formula),
            PackageKindFilter.Cask => query.Where(p => p.Kind == PackageKind.Cask),
            _ => query,
        };

        Packages.Clear();
        foreach (var p in query.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
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
