using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cocktails.Core;
using Cocktails.Localization;
using Cocktails.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cocktails.ViewModels;

/// <summary>
/// Base d'un écran de l'application. Fournit l'état partagé (occupé / message de
/// statut), la gestion centralisée des erreurs Homebrew via <see cref="RunAsync"/>,
/// et un chargement paresseux déclenché la première fois que l'écran devient actif.
/// </summary>
public abstract partial class ScreenViewModel : ViewModelBase
{
    protected IHomebrewService Homebrew { get; }

    protected ScreenViewModel(IHomebrewService homebrew)
    {
        Homebrew = homebrew;
        StatusMessage = L["Status.Ready"];
        Localizer.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Title));
            OnLanguageChanged();
        };
    }

    /// <summary>Accès raccourci aux traductions (langue courante).</summary>
    protected static Localizer L => Localizer.Instance;

    /// <summary>Clé de traduction du titre de l'écran.</summary>
    protected abstract string TitleKey { get; }

    /// <summary>Titre affiché dans l'en-tête de l'écran (traduit, mis à jour à chaud).</summary>
    public string Title => L[TitleKey];

    /// <summary>Appelé après un changement de langue (les écrans peuvent reconstruire leurs listes).</summary>
    protected virtual void OnLanguageChanged()
    {
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Demande de confirmation en attente (non nulle → dialogue affiché).</summary>
    [ObservableProperty]
    public partial ConfirmationRequest? Confirmation { get; set; }

    /// <summary>Dernières lignes du log brew de l'opération en cours (tail affiché dans l'overlay).</summary>
    public ObservableCollection<string> OutputLog { get; } = [];

    private const int MaxLogLines = 200;
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

    /// <summary>
    /// Marque l'écran comme « à recharger » : sa prochaine activation relancera le
    /// chargement initial (utilisé pour l'auto-rafraîchissement après monitoring).
    /// </summary>
    public void Invalidate() => _activated = false;

    /// <summary>Demande confirmation avant d'exécuter <paramref name="onConfirm"/>.</summary>
    protected void RequestConfirmation(string title, string message, string confirmLabel, Func<Task> onConfirm)
        => Confirmation = new ConfirmationRequest(title, message, confirmLabel, onConfirm);

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        var request = Confirmation;
        Confirmation = null;
        if (request is not null)
        {
            await request.OnConfirm();
        }
    }

    [RelayCommand]
    private void CancelConfirmation() => Confirmation = null;

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
            StatusMessage = L.Format("Error.Brew", ex.StandardError).Trim();
        }
        catch (Exception ex)
        {
            StatusMessage = L.Format("Error.Generic", ex.Message);
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

    /// <summary>Ajoute une ligne au terminal (borne le tail comme le flux de sortie).</summary>
    private void AppendLog(string line)
    {
        OutputLog.Add(line);
        while (OutputLog.Count > MaxLogLines)
        {
            OutputLog.RemoveAt(0);
        }
    }

    /// <summary>
    /// Exécute une commande saisie dans le terminal intégré. La saisie est analysée en
    /// arguments brew (<see cref="BrewCommandLine"/> ; aucun shell), et la sortie est
    /// <b>ajoutée</b> au terminal (sans l'effacer). Retourne les arguments exécutés (ou
    /// <c>null</c> si la saisie était invalide), pour que le shell décide d'un rechargement.
    /// </summary>
    public async Task<string[]?> RunTerminalCommandAsync(string input)
    {
        var args = BrewCommandLine.Parse(input);
        if (args is null)
        {
            AppendLog(L["Terminal.Invalid"]);
            return null;
        }

        var line = "brew " + string.Join(' ', args);
        var progress = new Progress<string>(AppendLog);
        await RunAsync(line, async () =>
        {
            AppendLog("$ " + line);
            var exit = await Homebrew.RunBrewAsync(args, progress);
            AppendLog(exit == 0 ? "✓" : $"✗ exit {exit}");
            StatusMessage = exit == 0 ? L["Terminal.Done"] : L.Format("Terminal.Failed", exit);
        });

        return args;
    }
}
