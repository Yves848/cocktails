namespace Cocktails.Core.Models;

/// <summary>Un service géré par Homebrew (<c>brew services</c>).</summary>
/// <param name="Name">Nom du service (= formula).</param>
/// <param name="Status">État brut : started / stopped / none / scheduled / error…</param>
/// <param name="User">Utilisateur sous lequel il tourne, si actif.</param>
/// <param name="File">Chemin du fichier de service (plist), si présent.</param>
public record BrewService(string Name, string Status, string? User, string? File)
{
    /// <summary>Vrai si le service est en cours d'exécution (ou planifié).</summary>
    public bool IsRunning => Status is "started" or "scheduled";

    /// <summary>Libellé lisible de l'état.</summary>
    public string StatusLabel => Status switch
    {
        "started" => "actif",
        "scheduled" => "planifié",
        "stopped" => "arrêté",
        "none" => "inactif",
        "error" => "erreur",
        _ => Status,
    };
}
