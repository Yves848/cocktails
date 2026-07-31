using System;
using System.ComponentModel;
using System.Globalization;

namespace Cocktails.Localization;

/// <summary>
/// Fournit les chaînes traduites pour la culture courante et permet de changer de langue
/// <b>à chaud</b> : l'indexeur <c>this[key]</c> est lié depuis le XAML (via <c>TrExtension</c>)
/// et depuis les view models ; un changement de langue émet <c>PropertyChanged("Item[]")</c>,
/// ce qui rafraîchit toutes les liaisons. Singleton (<see cref="Instance"/>).
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    public static Localizer Instance { get; } = new();

    private Localizer()
    {
    }

    /// <summary>Langue effectivement résolue (jamais <see cref="AppLanguage.System"/>).</summary>
    public AppLanguage Current { get; private set; } = ResolveSystem();

    /// <summary>Traduction pour la clé donnée, dans la langue courante.</summary>
    public string this[string key] => Strings.Get(key, Current);

    /// <summary>Émis après un changement de langue (pour reconstruire les listes non liées).</summary>
    public event EventHandler? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Applique une préférence de langue (résout <see cref="AppLanguage.System"/>).</summary>
    public void SetLanguage(AppLanguage preference)
    {
        var resolved = preference == AppLanguage.System ? ResolveSystem() : preference;
        if (resolved == Current)
        {
            return;
        }

        Current = resolved;
        // Notifie le changement de langue. Les liaisons {loc:Tr} observent la propriété
        // Current (via un converter) : une notification de propriété classique rafraîchit
        // de façon fiable même la vue déjà affichée. "Item[]" couvre les liaisons d'indexeur.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raccourci pour formater une chaîne traduite avec des paramètres.</summary>
    public string Format(string key, params object?[] args)
        => string.Format(CultureInfo.CurrentCulture, this[key], args);

    private static AppLanguage ResolveSystem() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "fr" => AppLanguage.French,
            "es" => AppLanguage.Spanish,
            "de" => AppLanguage.German,
            _ => AppLanguage.English,
        };
}
