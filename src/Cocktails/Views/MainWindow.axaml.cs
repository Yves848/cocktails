using Avalonia.Controls;
using Avalonia.Input;

namespace Cocktails.Views;

public partial class MainWindow : Window
{
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
    }
}
