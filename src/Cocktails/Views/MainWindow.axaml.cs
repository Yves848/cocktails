using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cocktails.ViewModels;

namespace Cocktails.Views;

public partial class MainWindow : Window
{
    // Écran dont on suit le log pour l'auto-défilement.
    private ScreenViewModel? _observedScreen;
    private static readonly TimeSpan SplashHold = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan SplashFade = TimeSpan.FromMilliseconds(500);

    // État du redimensionnement manuel en cours (fenêtre sans chrome).
    private WindowEdge? _resizeEdge;
    private PixelPoint _resizeStartPointer;
    private PixelPoint _resizeStartPos;
    private Size _resizeStartSize;

    public MainWindow()
    {
        InitializeComponent();

        // Fenêtre sans chrome : l'en-tête sert de zone de déplacement.
        DragArea.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        };

        CloseBtn.Click += (_, _) => Close();
        MinBtn.Click += (_, _) => WindowState = WindowState.Minimized;
        MaxBtn.Click += (_, _) => WindowState =
            WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        // Redimensionnement manuel : BeginResizeDrag n'est pas fiable sur une fenêtre
        // sans chrome (macOS), on gère donc la taille nous-mêmes via capture du pointeur.
        WireResize(ResizeT, WindowEdge.North);
        WireResize(ResizeB, WindowEdge.South);
        WireResize(ResizeL, WindowEdge.West);
        WireResize(ResizeR, WindowEdge.East);
        WireResize(ResizeTL, WindowEdge.NorthWest);
        WireResize(ResizeTR, WindowEdge.NorthEast);
        WireResize(ResizeBL, WindowEdge.SouthWest);
        WireResize(ResizeBR, WindowEdge.SouthEast);

        AddHandler(PointerMovedEvent, OnResizePointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnResizePointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // Raccourcis clavier globaux (tunnel : indépendants du focus courant).
        AddHandler(KeyDownEvent, OnGlobalKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        Opened += OnOpened;
    }

    /// <summary>
    /// Raccourcis clavier au niveau fenêtre. Choix pensés pour un clavier AZERTY Apple
    /// (touches lettres + F1 + ⌘, standard) — cf. écran Aide qui les documente.
    /// </summary>
    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        var meta = e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        var vm = DataContext as MainViewModel;

        if (meta && e.Key == Key.Q)
        {
            (Application.Current as App)?.Quit();
            e.Handled = true;
        }
        else if (meta && e.Key == Key.W)
        {
            Close();
            e.Handled = true;
        }
        else if (meta && e.Key == Key.M)
        {
            WindowState = WindowState.Minimized;
            e.Handled = true;
        }
        else if (meta && (e.Key == Key.OemComma || e.KeySymbol == ","))
        {
            vm?.SelectScreen("Nav.Settings");
            e.Handled = true;
        }
        else if (e.Key == Key.F1)
        {
            vm?.SelectScreen("Nav.Help");
            e.Handled = true;
        }
        else if (meta && e.Key == Key.F)
        {
            // ⌘F : focus le champ de filtre de l'écran courant.
            FindInContent<TextBox>("FilterBox")?.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            // Tab / ⇧Tab : bascule le focus entre la zone menu (nav) et la zone grille
            // (tuiles). Les contrôles d'en-tête restent joignables par leurs raccourcis.
            ToggleZoneFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Space && IsFocusInTiles()
                 && vm?.CurrentScreen is PackageListViewModel plvm && plvm.SelectedItem is { } sp)
        {
            // Espace : (dé)coche la tuile focalisée (opérations par lot).
            sp.IsChecked = !sp.IsChecked;
            e.Handled = true;
        }
        else if ((e.Key == Key.Up || e.Key == Key.Down) && IsFocusInTiles())
        {
            // Le WrapPanel ne gère pas le saut de rangée : on le fait géométriquement
            // (tuile de la rangée voisine dont la colonne est la plus proche).
            MoveTilesVertically(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
        }
        else if (meta && TryGetDigitIndex(e.Key) is { } index)
        {
            // ⌘1…⌘8 → onglets. La rangée du haut (Key.D1…) et le pavé numérique
            // (Key.NumPad1…) sont acceptés : sur AZERTY Apple la rangée du haut exige
            // ⇧ pour un vrai chiffre, mais la touche physique reste Key.Dn — donc
            // ⌘+touche fonctionne sans ⇧, et le pavé numérique offre de vrais chiffres.
            vm?.SelectByIndex(index);
            e.Handled = true;
        }
    }

    // --- Navigation clavier par zones (menu ↔ grille) ------------------------

    private T? FindInContent<T>(string name) where T : Control
        => this.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);

    private ListBox? TilesList() => FindInContent<ListBox>("List");

    private bool IsFocusInTiles()
    {
        var tiles = TilesList();
        return tiles is not null
               && FocusManager?.GetFocusedElement() is Visual focused
               && (focused == tiles || tiles.IsVisualAncestorOf(focused));
    }

    /// <summary>
    /// Déplace la sélection d'une rangée dans la grille (WrapPanel) : cherche, parmi les
    /// tuiles de la rangée immédiatement au-dessus/au-dessous, celle dont la position
    /// horizontale est la plus proche. <paramref name="dir"/> = +1 (bas) / -1 (haut).
    /// </summary>
    private void MoveTilesVertically(int dir)
    {
        var list = TilesList();
        if (list is null || list.ItemCount == 0)
        {
            return;
        }

        var cur = list.SelectedIndex;
        if (cur < 0)
        {
            list.SelectedIndex = 0;
            FocusTile(list, 0);
            return;
        }

        if (list.ContainerFromIndex(cur) is not Control curContainer)
        {
            return;
        }

        var curX = curContainer.Bounds.X;
        var curY = curContainer.Bounds.Y;

        var best = -1;
        var bestScore = double.MaxValue;
        for (var i = 0; i < list.ItemCount; i++)
        {
            if (i == cur || list.ContainerFromIndex(i) is not Control c)
            {
                continue;
            }

            var b = c.Bounds;
            // La cible doit être sur une autre rangée dans le sens demandé.
            if (dir > 0 && b.Y <= curY + 1)
            {
                continue;
            }

            if (dir < 0 && b.Y >= curY - 1)
            {
                continue;
            }

            // Rangée la plus proche d'abord, puis colonne la plus proche.
            var score = (Math.Abs(b.Y - curY) * 100000) + Math.Abs(b.X - curX);
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        if (best >= 0)
        {
            list.SelectedIndex = best;
            FocusTile(list, best);
        }
    }

    private static void FocusTile(ListBox list, int index)
    {
        list.ScrollIntoView(index);
        (list.ContainerFromIndex(index) as Control)?.Focus();
    }

    /// <summary>Bascule le focus entre la grille de tuiles (zone 2) et le menu (zone 1).</summary>
    private void ToggleZoneFocus()
    {
        var nav = this.FindControl<ListBox>("NavList");
        if (IsFocusInTiles())
        {
            nav?.Focus();
            return;
        }

        if (TilesList() is { ItemCount: > 0 } tiles)
        {
            var idx = tiles.SelectedIndex < 0 ? 0 : tiles.SelectedIndex;
            tiles.SelectedIndex = idx;
            FocusTile(tiles, idx);
        }
        else
        {
            nav?.Focus();
        }
    }

    /// <summary>Indice 0-based de l'onglet pour une touche chiffre 1…8, sinon null.</summary>
    private static int? TryGetDigitIndex(Key key) => key switch
    {
        Key.D1 or Key.NumPad1 => 0,
        Key.D2 or Key.NumPad2 => 1,
        Key.D3 or Key.NumPad3 => 2,
        Key.D4 or Key.NumPad4 => 3,
        Key.D5 or Key.NumPad5 => 4,
        Key.D6 or Key.NumPad6 => 5,
        Key.D7 or Key.NumPad7 => 6,
        Key.D8 or Key.NumPad8 => 7,
        _ => null,
    };

    private void WireResize(Control handle, WindowEdge edge)
    {
        handle.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            _resizeEdge = edge;
            _resizeStartPointer = this.PointToScreen(e.GetPosition(this));
            _resizeStartPos = Position;
            _resizeStartSize = Bounds.Size;
            e.Pointer.Capture(this);
            e.Handled = true;
        };
    }

    private void OnResizePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizeEdge is not { } edge)
        {
            return;
        }

        var scale = RenderScaling;
        var current = this.PointToScreen(e.GetPosition(this));
        double dx = (current.X - _resizeStartPointer.X) / scale;
        double dy = (current.Y - _resizeStartPointer.Y) / scale;

        var left = edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest;
        var right = edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast;
        var top = edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast;
        var bottom = edge is WindowEdge.South or WindowEdge.SouthWest or WindowEdge.SouthEast;

        double newW = _resizeStartSize.Width + (right ? dx : left ? -dx : 0);
        double newH = _resizeStartSize.Height + (bottom ? dy : top ? -dy : 0);
        newW = Math.Max(MinWidth, newW);
        newH = Math.Max(MinHeight, newH);

        int posX = _resizeStartPos.X;
        int posY = _resizeStartPos.Y;
        if (left)
        {
            posX += (int)Math.Round((_resizeStartSize.Width - newW) * scale);
        }

        if (top)
        {
            posY += (int)Math.Round((_resizeStartSize.Height - newH) * scale);
        }

        Width = newW;
        Height = newH;
        Position = new PixelPoint(posX, posY);
        e.Handled = true;
    }

    private void OnResizePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_resizeEdge is null)
        {
            return;
        }

        _resizeEdge = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    // --- Auto-défilement du log de l'overlay ---------------------------------

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnShellPropertyChanged;
            HookLog(vm.CurrentScreen);
        }
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentScreen) && DataContext is MainViewModel vm)
        {
            HookLog(vm.CurrentScreen);
        }
    }

    private void HookLog(ScreenViewModel? screen)
    {
        if (_observedScreen is not null)
        {
            _observedScreen.OutputLog.CollectionChanged -= OnLogChanged;
        }

        _observedScreen = screen;

        if (_observedScreen is not null)
        {
            _observedScreen.OutputLog.CollectionChanged += OnLogChanged;
        }
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Dispatcher.UIThread.Post(() => LogScroll?.ScrollToEnd(), DispatcherPriority.Background);

    private void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;

        // Laisse le splash visible un court instant, puis fondu, puis retrait complet
        // de l'arbre visuel (ce qui stoppe l'animation du shaker via son détachement).
        DispatcherTimer.RunOnce(() =>
        {
            Splash.Opacity = 0;
            DispatcherTimer.RunOnce(
                () => (Splash.Parent as Panel)?.Children.Remove(Splash),
                SplashFade);
        }, SplashHold);
    }
}
