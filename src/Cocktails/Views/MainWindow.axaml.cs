using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Cocktails.Views;

public partial class MainWindow : Window
{
    private static readonly TimeSpan SplashHold = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan SplashFade = TimeSpan.FromMilliseconds(500);

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

        Opened += OnOpened;
    }

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
