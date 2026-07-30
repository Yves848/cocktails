using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    // Noms cochés (persistés à travers les changements de filtre) et garde anti-réentrance.
    private readonly HashSet<string> _checkedNames = new(StringComparer.Ordinal);
    private bool _suppressCheck;

    // Séquence pour ignorer les résultats de détail obsolètes (sélection rapide).
    private int _detailToken;

    protected PackageListViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    /// <summary>Vue affichée (filtrée par <see cref="FilterText"/>, triée par nom).</summary>
    public ObservableCollection<SelectablePackage> Packages { get; } = [];

    /// <summary>Nombre de lignes cochées (pilote la barre d'actions par lot).</summary>
    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    /// <summary>Vrai dès qu'au moins une ligne est cochée.</summary>
    public bool AnySelected => SelectedCount > 0;

    partial void OnSelectedCountChanged(int value) => OnPropertyChanged(nameof(AnySelected));

    /// <summary>Ligne mise en surbrillance (pilote le volet de détail).</summary>
    [ObservableProperty]
    public partial SelectablePackage? SelectedItem { get; set; }

    partial void OnSelectedItemChanged(SelectablePackage? value) => SelectedPackage = value?.Package;

    /// <summary>Packages actuellement cochés (dans l'ordre du jeu complet).</summary>
    protected IReadOnlyList<Package> CheckedPackages()
        => _all.Where(p => _checkedNames.Contains(p.Name)).ToList();

    /// <summary>Décoche toutes les lignes.</summary>
    [RelayCommand]
    protected void ClearChecks()
    {
        _suppressCheck = true;
        foreach (var w in Packages)
        {
            w.IsChecked = false;
        }

        _suppressCheck = false;
        _checkedNames.Clear();
        SelectedCount = 0;
    }

    private void OnItemCheckChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressCheck || e.PropertyName != nameof(SelectablePackage.IsChecked))
        {
            return;
        }

        var w = (SelectablePackage)sender!;
        if (w.IsChecked)
        {
            _checkedNames.Add(w.Package.Name);
        }
        else
        {
            _checkedNames.Remove(w.Package.Name);
        }

        SelectedCount = _checkedNames.Count;
    }

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

    /// <summary>Paquets installés qui dépendent du package sélectionné (via <c>brew uses</c>).</summary>
    public ObservableCollection<string> Dependents { get; } = [];

    /// <summary>Prédicat de filtre additionnel optionnel (ex. « racines seulement »).</summary>
    protected Func<Package, bool>? ExtraFilter { get; set; }

    /// <summary>Remplace le jeu complet puis applique filtre + tri (réinitialise les coches).</summary>
    protected void Replace(IReadOnlyList<Package> items)
    {
        _checkedNames.Clear();
        SelectedCount = 0;
        _all.Clear();
        _all.AddRange(items);
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    protected void ApplyFilter()
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

        if (ExtraFilter is { } extra)
        {
            query = query.Where(extra);
        }

        foreach (var w in Packages)
        {
            w.PropertyChanged -= OnItemCheckChanged;
        }

        Packages.Clear();
        foreach (var p in query.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            var w = new SelectablePackage(p) { IsChecked = _checkedNames.Contains(p.Name) };
            w.PropertyChanged += OnItemCheckChanged;
            Packages.Add(w);
        }
    }

    /// <summary>Vide la sélection et le détail (ex. après une désinstallation).</summary>
    protected void ClearSelection()
    {
        SelectedItem = null;
        SelectedPackage = null;
        Details = null;
        Dependents.Clear();
    }

    partial void OnSelectedPackageChanged(Package? value) => _ = LoadDetailsAsync(value);

    /// <summary>Dépendants installés à charger pour le détail (aucun par défaut).</summary>
    protected virtual Task<IReadOnlyList<string>> LoadDependentsAsync(Package package)
        => Task.FromResult<IReadOnlyList<string>>([]);

    private async Task LoadDetailsAsync(Package? package)
    {
        var token = ++_detailToken;
        if (package is null)
        {
            Details = null;
            Dependents.Clear();
            return;
        }

        IsLoadingDetails = true;
        try
        {
            var details = await Homebrew.GetInfoAsync(package.Name);
            var dependents = await LoadDependentsAsync(package);
            if (token == _detailToken)
            {
                Details = details;
                Dependents.Clear();
                foreach (var d in dependents)
                {
                    Dependents.Add(d);
                }
            }
        }
        catch (Exception)
        {
            if (token == _detailToken)
            {
                Details = null;
                Dependents.Clear();
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
