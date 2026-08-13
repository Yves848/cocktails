namespace Cocktails.ViewModels;

/// <summary>
/// Demande de mot de passe administrateur affichée en superposition, émise quand
/// <c>brew</c> lance un installeur qui réclame <c>sudo</c>.
/// </summary>
/// <param name="Title">Titre du dialogue.</param>
/// <param name="Message">Explication (ou avertissement si la saisie précédente a échoué).</param>
/// <param name="IsRetry">Vrai si le mot de passe précédent a été refusé.</param>
public sealed record PasswordRequest(string Title, string Message, bool IsRetry);
