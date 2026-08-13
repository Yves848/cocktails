namespace Cocktails.Core.Sudo;

/// <summary>
/// Ce dont <see cref="HomebrewService"/> a besoin pour donner à <c>brew</c> les moyens
/// d'obtenir les droits administrateur : le chemin du programme askpass à exporter, et le
/// bornage des opérations (une commande brew = une opération, cf.
/// <see cref="AskpassBroker.BeginOperation"/>).
/// </summary>
public interface ISudoAskpass
{
    /// <summary>
    /// Programme à exporter dans <c>SUDO_ASKPASS</c>, ou <c>null</c> si le mécanisme n'est
    /// pas disponible — auquel cas rien n'est exporté et brew se comporte comme avant.
    /// </summary>
    string? HelperPath { get; }

    /// <summary>Signale le début d'une commande brew susceptible d'appeler <c>sudo</c>.</summary>
    void BeginOperation();
}
