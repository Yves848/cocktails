using System.Threading.Tasks;

namespace Cocktails.Services;

/// <summary>Envoi d'une notification système.</summary>
public interface INotifier
{
    Task NotifyAsync(string title, string message);
}
