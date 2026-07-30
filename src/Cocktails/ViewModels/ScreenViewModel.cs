using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cocktails.ViewModels;

/// <summary>
/// Base d'un écran de l'application. Fournit l'état partagé (occupé / message de
/// statut), la gestion centralisée des erreurs Homebrew via <see cref="RunAsync"/>,
/// et un chargement paresseux déclenché la première fois que l'écran devient actif.
/// </summary>
public abstract partial class ScreenViewModel : ViewModelBase
{
    protected IHomebrewService Homebrew { get; }

    protected ScreenViewModel(IHomebrewService homebrew) => Homebrew = homebrew;

    /// <summary>Titre affiché dans l'en-tête de l'écran.</summary>
    public abstract string Title { get; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Prêt.";

    /// <summary>Dernières lignes du log brew de l'opération en cours (tail affiché dans l'overlay).</summary>
    public ObservableCollection<string> OutputLog { get; } = [];

    private const int MaxLogLines = 14;
    private bool _activated;

    /// <summary>
    /// Appelé par le shell quand l'écran devient actif. Déclenche le chargement initial
    /// une seule fois (les rafraîchissements ultérieurs passent par les commandes de l'écran).
    /// </summary>
    public async Task ActivateAsync()
    {
        if (_activated)
        {
            return;
        }

        _activated = true;
        await OnFirstActivatedAsync();
    }

    /// <summary>Chargement initial de l'écran (aucun par défaut).</summary>
    protected virtual Task OnFirstActivatedAsync() => Task.CompletedTask;

    /// <summary>Exécute une opération en gérant l'état occupé et les erreurs Homebrew.</summary>
    protected async Task RunAsync(string busyMessage, Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = busyMessage;
        try
        {
            await action();
        }
        catch (HomebrewException ex)
        {
            StatusMessage = $"Erreur brew : {ex.StandardError}".Trim();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Variante de <see cref="RunAsync"/> pour les commandes brew longues : alimente
    /// <see cref="OutputLog"/> avec le flux de sortie (tail des dernières lignes). Le
    /// reporter est créé sur le thread UI, donc <c>OutputLog</c> est mis à jour dessus.
    /// </summary>
    protected Task RunWithOutputAsync(string busyMessage, Func<IProgress<string>, Task> action)
    {
        OutputLog.Clear();
        var progress = new Progress<string>(line =>
        {
            OutputLog.Add(line);
            while (OutputLog.Count > MaxLogLines)
            {
                OutputLog.RemoveAt(0);
            }
        });

        return RunAsync(busyMessage, () => action(progress));
    }
}
