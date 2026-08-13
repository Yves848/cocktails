namespace Cocktails.Core.Sudo;

/// <summary>
/// Lecture des lignes par lesquelles <c>sudo</c> annonce qu'il n'a pas pu obtenir le mot de
/// passe. Avec le courtier askpass en place, elles ne devraient plus apparaître ; les voir
/// signifie que le mécanisme est indisponible, ce que l'interface explique alors à
/// l'utilisateur plutôt que de lui laisser le message brut.
/// </summary>
public static class SudoOutput
{
    public static bool IsPasswordFailure(string line) =>
        line.StartsWith("sudo:", StringComparison.Ordinal)
        && (line.Contains("a terminal is required", StringComparison.Ordinal)
            || line.Contains("a password is required", StringComparison.Ordinal));
}
