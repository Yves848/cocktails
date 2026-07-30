using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>Écran « Rechercher » : recherche dans Homebrew et installation.</summary>
public partial class SearchViewModel : PackageListViewModel
{
    public SearchViewModel(IHomebrewService homebrew) : base(homebrew)
    {
        StatusMessage = "Saisissez un terme puis lancez la recherche.";
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public SearchViewModel() : this(new DesignHomebrewService())
    {
    }

    public override string Title => "Rechercher";

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [RelayCommand]
    private Task SearchAsync()
    {
        var query = SearchQuery.Trim();
        if (query.Length == 0)
        {
            return Task.CompletedTask;
        }

        return RunAsync($"Recherche de « {query} »…", async () =>
        {
            var results = await Homebrew.SearchAsync(query);

            // Marque les résultats déjà installés (brew search ne donne pas cet état) :
            // on croise avec la liste des installés pour reporter leur version.
            var installedVersions = (await Homebrew.GetInstalledAsync())
                .ToDictionary(p => p.Name, p => p.InstalledVersion);
            var marked = results
                .Select(r => installedVersions.TryGetValue(r.Name, out var v) && v is not null
                    ? r with { InstalledVersion = v }
                    : r)
                .ToList();

            Replace(marked);
            var installedCount = marked.Count(p => p.IsInstalled);
            StatusMessage = installedCount > 0
                ? $"{marked.Count} résultat(s) — {installedCount} déjà installé(s)."
                : $"{marked.Count} résultat(s) pour « {query} ».";
        });
    }

    [RelayCommand]
    private Task InstallAsync(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync($"Installation de « {package.Name} »…", async progress =>
        {
            await Homebrew.InstallAsync(package.Name, progress);
            StatusMessage = $"« {package.Name} » installé.";
        });
    }
}
