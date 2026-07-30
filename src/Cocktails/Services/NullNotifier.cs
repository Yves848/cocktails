using System.Threading.Tasks;

namespace Cocktails.Services;

/// <summary>Notifier neutre (design-time / tests / plateformes sans support).</summary>
public sealed class NullNotifier : INotifier
{
    public Task NotifyAsync(string title, string message) => Task.CompletedTask;
}
