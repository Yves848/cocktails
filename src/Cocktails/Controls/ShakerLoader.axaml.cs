using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cocktails.Controls;

/// <summary>
/// Indicateur d'activité vectoriel. Le shaker oscille autour de son centre et ne
/// dépend d'aucun format d'animation externe.
/// </summary>
public partial class ShakerLoader : UserControl
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);
    private const double CycleSeconds = 0.46;
    private const double MaximumAngle = 18;

    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private readonly RotateTransform _shakerRotation;
    private bool _isAttached;

    public ShakerLoader()
    {
        InitializeComponent();

        var shaker = this.FindControl<Viewbox>("AnimatedShaker")
            ?? throw new InvalidOperationException("Le visuel du shaker est introuvable.");
        _shakerRotation = (RotateTransform)(shaker.RenderTransform
            ?? throw new InvalidOperationException("Le transform du shaker est introuvable."));

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = FrameInterval,
        };
        _timer.Tick += OnAnimationTick;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        UpdateAnimationState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        StopAnimation();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty)
        {
            UpdateAnimationState();
        }
    }

    private void UpdateAnimationState()
    {
        if (_isAttached && IsVisible)
        {
            if (!_timer.IsEnabled)
            {
                _stopwatch.Restart();
                _timer.Start();
            }
        }
        else
        {
            StopAnimation();
        }
    }

    private void StopAnimation()
    {
        _timer.Stop();
        _stopwatch.Reset();
        _shakerRotation.Angle = 0;
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var phase = _stopwatch.Elapsed.TotalSeconds / CycleSeconds * Math.Tau;

        // La petite harmonique donne un impact plus sec aux changements de direction.
        _shakerRotation.Angle =
            MaximumAngle * (Math.Sin(phase) + 0.12 * Math.Sin(phase * 3)) / 1.12;
    }
}
