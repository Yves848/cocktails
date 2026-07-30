using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Cocktails.Controls;

/// <summary>
/// Affiche l'icône d'un package. Comme Homebrew ne fournit pas d'icône, on récupère
/// le favicon du site officiel (<c>Homepage</c>) via le proxy favicons de
/// yg-devworks.com. En cas d'absence ou d'échec (non-2xx), on retombe sur une pastille
/// avec l'initiale du type (<c>FallbackText</c>). Le résultat est mis en cache par
/// domaine pour la durée de la session. Contrat du proxy : cf. docs/proxy-favicons.md.
/// </summary>
public class AppIcon : UserControl
{
    /// <summary>Base du proxy favicons (cf. docs/proxy-favicons.md).</summary>
    private const string FaviconProxy = "https://favicons.yg-devworks.com";
    private const int IconSize = 64;

    public static readonly StyledProperty<string?> HomepageProperty =
        AvaloniaProperty.Register<AppIcon, string?>(nameof(Homepage));

    public static readonly StyledProperty<string?> FallbackTextProperty =
        AvaloniaProperty.Register<AppIcon, string?>(nameof(FallbackText));

    public string? Homepage
    {
        get => GetValue(HomepageProperty);
        set => SetValue(HomepageProperty, value);
    }

    public string? FallbackText
    {
        get => GetValue(FallbackTextProperty);
        set => SetValue(FallbackTextProperty, value);
    }

    private static readonly HttpClient Http = CreateClient();
    private static readonly Dictionary<string, Bitmap?> Cache = new();

    private readonly Image _image = new() { Stretch = Stretch.Uniform };
    private readonly TextBlock _fallback = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 15,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.Parse("#9AA0B4")),
    };

    public AppIcon()
    {
        Content = new Border
        {
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.Parse("#10FFFFFF")),
            ClipToBounds = true,
            Child = new Panel { Children = { _fallback, _image } },
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FallbackTextProperty)
        {
            _fallback.Text = FallbackText;
        }
        else if (change.Property == HomepageProperty)
        {
            _ = LoadAsync(Homepage);
        }
    }

    private async Task LoadAsync(string? homepage)
    {
        _image.Source = null;
        _fallback.IsVisible = true;

        var domain = ExtractDomain(homepage);
        if (domain is null)
        {
            return;
        }

        var url = $"{FaviconProxy}/{Uri.EscapeDataString(domain)}?sz={IconSize}";
        if (!Cache.TryGetValue(url, out var bitmap))
        {
            try
            {
                var bytes = await Http.GetByteArrayAsync(url).ConfigureAwait(true);
                using var stream = new MemoryStream(bytes);
                bitmap = new Bitmap(stream);
            }
            catch (Exception)
            {
                bitmap = null;
            }

            Cache[url] = bitmap;
        }

        // Ignore si la sélection a changé entre-temps.
        if (Homepage != homepage)
        {
            return;
        }

        if (bitmap is not null)
        {
            _image.Source = bitmap;
            _fallback.IsVisible = false;
        }
    }

    private static string? ExtractDomain(string? homepage)
        => !string.IsNullOrWhiteSpace(homepage)
           && Uri.TryCreate(homepage, UriKind.Absolute, out var uri)
            ? uri.Host
            : null;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
        client.DefaultRequestHeaders.Add("User-Agent", "Cocktails/1.0 (Homebrew GUI)");
        return client;
    }
}
