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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    public partial string SearchQuery { get; set; } = string.Empty;

    private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchQuery);

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private Task SearchAsync()
    {
        var query = SearchQuery.Trim();
        if (query.Length == 0)
        {
            return Task.CompletedTask;
        }

        return RunAsync(L["Status.Searching"], async () =>
        {
            var results = await Homebrew.SearchAsync(query);

            // Marque les résultats déjà installés (brew search ne donne pas cet état) :
            // on croise avec la liste des installés pour reporter leur version.
            // Un même nom peut être installé en formula ET en cask (ex. powershell) :
            // regrouper par nom évite une exception de clé dupliquée dans le dictionnaire.
            var installedVersions = (await Homebrew.GetInstalledAsync())
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.First().InstalledVersion);
            var marked = results
                .Select(r => installedVersions.TryGetValue(r.Name, out var v) && v is not null
                    ? r with { InstalledVersion = v }
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
            await Homebrew.InstallAsync(package.Name, progress);
            StatusMessage = L.Format("Status.Installed", package.Name);
        });
    }
}
