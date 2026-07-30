using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
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

        if (meta && e.Key == Key.W)
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
            vm?.SelectScreen("Réglages");
            e.Handled = true;
        }
        else if (e.Key == Key.F1)
        {
            vm?.SelectScreen("Aide");
            e.Handled = true;
        }
    }

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
