using BAAZ.CMMS.App.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.StatusMarker;

/// <summary>Круговая цветная метка с подсказкой из <see cref="StatusMarkerFactory"/>.</summary>
public sealed partial class StatusMarker : UserControl
{
    private const double DefaultSize = 10;
    private const double LargeSize = 14;

    public StatusMarker()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyVisuals();
        ActualThemeChanged += (_, _) => ApplyVisuals();
    }

    public static readonly DependencyProperty ColorKeyProperty =
        DependencyProperty.Register(
            nameof(ColorKey),
            typeof(string),
            typeof(StatusMarker),
            new PropertyMetadata(StatusBadgePalette.DefaultBackgroundKey, OnVisualPropertyChanged));

    public string ColorKey
    {
        get => (string)GetValue(ColorKeyProperty);
        set => SetValue(ColorKeyProperty, value);
    }

    public static readonly DependencyProperty ToolTipTextProperty =
        DependencyProperty.Register(
            nameof(ToolTipText),
            typeof(string),
            typeof(StatusMarker),
            new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

    public string ToolTipText
    {
        get => (string)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    public static readonly DependencyProperty IsLargeProperty =
        DependencyProperty.Register(
            nameof(IsLarge),
            typeof(bool),
            typeof(StatusMarker),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    /// <summary>Увеличенный размер метки (14×14 вместо 10×10).</summary>
    public bool IsLarge
    {
        get => (bool)GetValue(IsLargeProperty);
        set => SetValue(IsLargeProperty, value);
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusMarker marker)
            marker.ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        var size = IsLarge ? LargeSize : DefaultSize;
        MarkerDot.Width = size;
        MarkerDot.Height = size;
        MarkerDot.Fill = StatusBadgePalette.ResolveBackgroundBrush(ColorKey, ActualTheme);

        ToolTipService.SetToolTip(
            this,
            string.IsNullOrWhiteSpace(ToolTipText) ? null : new ToolTip { Content = ToolTipText });
    }
}
