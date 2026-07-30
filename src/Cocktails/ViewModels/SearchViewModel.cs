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
            Replace(results);
            StatusMessage = $"{results.Count} résultat(s) pour « {query} ».";
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
