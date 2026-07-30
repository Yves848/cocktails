using System;
using System.Threading.Tasks;

namespace Cocktails.ViewModels;

/// <summary>
/// Demande de confirmation affichée en superposition (titre, message, libellé du bouton
/// de confirmation et action à exécuter si l'utilisateur confirme).
/// </summary>
public sealed record ConfirmationRequest(
    string Title,
    string Message,
    string ConfirmLabel,
    Func<Task> OnConfirm);
