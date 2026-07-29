using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cocktails.Core;
using Cocktails.Core.Models;

namespace Cocktails.ViewModels;

/// <summary>Écran présentant une liste de packages.</summary>
public abstract class PackageListViewModel : ScreenViewModel
{
    protected PackageListViewModel(IHomebrewService homebrew) : base(homebrew)
    {
    }

    public ObservableCollection<Package> Packages { get; } = [];

    /// <summary>Remplace le contenu de la liste par <paramref name="items"/>.</summary>
    protected void Replace(IReadOnlyList<Package> items)
    {
        Packages.Clear();
        foreach (var p in items)
        {
            Packages.Add(p);
        }
    }
}
