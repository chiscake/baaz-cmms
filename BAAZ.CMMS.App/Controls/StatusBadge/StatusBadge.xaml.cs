using BAAZ.CMMS.App.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.StatusBadge;

/// <summary>Бейдж статуса с цветом из <see cref="StatusBadgeFactory"/> (Material palette).</summary>
public sealed partial class StatusBadge : UserControl
{
    private const double RoundedCornerRadius = 4;
    private const double PillCornerRadius = 9999;

    public StatusBadge()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplyShape();
            ApplyBrushes();
        };
        ActualThemeChanged += (_, _) => ApplyBrushes();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty BackgroundKeyProperty =
        DependencyProperty.Register(
            nameof(BackgroundKey),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(StatusBadgeFactory.DefaultBackgroundKey, OnVisualPropertyChanged));

    public string BackgroundKey
    {
        get => (string)GetValue(BackgroundKeyProperty);
        set => SetValue(BackgroundKeyProperty, value);
    }

    public static readonly DependencyProperty ForegroundKeyProperty =
        DependencyProperty.Register(
            nameof(ForegroundKey),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(StatusBadgeFactory.DefaultForegroundKey, OnVisualPropertyChanged));

    public string ForegroundKey
    {
        get => (string)GetValue(ForegroundKeyProperty);
        set => SetValue(ForegroundKeyProperty, value);
    }

    public static readonly DependencyProperty IsPillProperty =
        DependencyProperty.Register(
            nameof(IsPill),
            typeof(bool),
            typeof(StatusBadge),
            new PropertyMetadata(false, OnShapePropertyChanged));

    /// <summary>Капсульная форма (полностью скруглённые края) вместо скруглённого прямоугольника.</summary>
    public bool IsPill
    {
        get => (bool)GetValue(IsPillProperty);
        set => SetValue(IsPillProperty, value);
    }

    private static void OnShapePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
            badge.ApplyShape();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
            badge.ApplyBrushes();
    }

    private void ApplyShape()
    {
        var radius = IsPill ? PillCornerRadius : RoundedCornerRadius;
        BadgeBorder.CornerRadius = new CornerRadius(radius);
    }

    private void ApplyBrushes()
    {
        BadgeText.Text = Label;
        BadgeBorder.Background = StatusBadgePalette.ResolveBackgroundBrush(BackgroundKey, ActualTheme);
        BadgeText.Foreground = StatusBadgePalette.ResolveForegroundBrush(ForegroundKey, ActualTheme);
    }
}
