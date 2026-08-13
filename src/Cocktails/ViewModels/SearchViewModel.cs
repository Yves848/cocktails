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
    }

    /// <summary>Constructeur design-time (previewer XAML).</summary>
    public SearchViewModel() : this(new DesignHomebrewService())
    {
    }

    protected override string TitleKey => "Nav.Search";

    /// <summary>Plafond d'enrichissement (brew info) pour borner le coût d'une recherche large.</summary>
    private const int EnrichCap = 200;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [RelayCommand]
    private Task SearchAsync()
    {
        var query = SearchQuery.Trim();
        if (query.Length == 0)
        {
            // Recherche vide = on remet la liste à zéro (efface les résultats précédents).
            ClearSelection();
            Replace([]);
            StatusMessage = string.Empty;
            return Task.CompletedTask;
        }

        return RunAsync(L["Status.Searching"], async () =>
        {
            var results = await Homebrew.SearchAsync(query);

            // Enrichit chaque résultat (icône via homepage, version disponible, description,
            // et état installé) en UN seul appel brew info groupé — l'API Homebrew étant en
            // cache, c'est rapide même pour beaucoup de noms. Best-effort : si l'info échoue,
            // on garde les résultats bruts (nom + type). La clé (nom, type) gère le cas d'un
            // même nom en formula ET en cask.
            var infoByKey = new Dictionary<(string, PackageKind), Package>();
            try
            {
                var names = results.Select(r => r.Name).Distinct().Take(EnrichCap).ToList();
                foreach (var i in await Homebrew.GetInfoForAsync(names))
                {
                    infoByKey[(i.Name, i.Kind)] = i;
                }
            }
            catch (HomebrewException)
            {
                // Enrichissement indisponible : on continue avec les résultats bruts.
            }

            var marked = results
                .Select(r => infoByKey.TryGetValue((r.Name, r.Kind), out var i)
                    ? r with
                    {
                        InstalledVersion = i.InstalledVersion,
                        LatestVersion = i.LatestVersion,
                        Description = i.Description,
                        Homepage = i.Homepage,
                    }
                    : r)
                .ToList();

            Replace(marked);
            var installedCount = marked.Count(p => p.IsInstalled);
            StatusMessage = L.Format("Status.SearchResults", marked.Count, installedCount);
        });
    }

    [RelayCommand]
    private Task InstallAsync(Package? package)
    {
        if (package is null)
        {
            return Task.CompletedTask;
        }

        return RunWithOutputAsync(L.Format("Status.Installing", package.Name), async progress =>
        {
            await Homebrew.InstallAsync(package.Name, package.Kind, progress);
            StatusMessage = L.Format("Status.Installed", package.Name);
        });
    }
}
