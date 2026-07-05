using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls;

/// <summary>
/// Компактный чекбокс фиксированного размера со скруглёнными углами.
/// Поддерживает двусторонний биндинг <see cref="IsChecked"/> и трёхсостоятельный режим.
/// </summary>
public sealed partial class CompactCheckBox : UserControl
{
    private bool _isPointerOver;
    private bool _isPressed;

    public CompactCheckBox()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerCanceled += OnPointerReleased;
        PointerEntered += (_, _) => { _isPointerOver = true; UpdateVisualState(); };
        PointerExited += (_, _) =>
        {
            _isPointerOver = false;
            _isPressed = false;
            UpdateVisualState();
        };
        KeyDown += OnKeyDown;
        Loaded += (_, _) => UpdateVisualState();
        IsEnabledChanged += (_, _) => UpdateVisualState();
    }

    // ── Dependency properties ───────────────────────────────────────────────────

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool?),
            typeof(CompactCheckBox),
            new PropertyMetadata(false, OnIsCheckedPropertyChanged));

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public static readonly DependencyProperty IsThreeStateProperty =
        DependencyProperty.Register(
            nameof(IsThreeState),
            typeof(bool),
            typeof(CompactCheckBox),
            new PropertyMetadata(false));

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    public static readonly DependencyProperty SuppressToggleProperty =
        DependencyProperty.Register(
            nameof(SuppressToggle),
            typeof(bool),
            typeof(CompactCheckBox),
            new PropertyMetadata(false));

    /// <summary>Не менять <see cref="IsChecked"/> по клику — только событие <see cref="Click"/>.</summary>
    public bool SuppressToggle
    {
        get => (bool)GetValue(SuppressToggleProperty);
        set => SetValue(SuppressToggleProperty, value);
    }

    public static readonly DependencyProperty BoxSizeProperty =
        DependencyProperty.Register(
            nameof(BoxSize),
            typeof(double),
            typeof(CompactCheckBox),
            new PropertyMetadata(16.0));

    public double BoxSize
    {
        get => (double)GetValue(BoxSizeProperty);
        set => SetValue(BoxSizeProperty, value);
    }

    public static readonly DependencyProperty BoxCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(BoxCornerRadius),
            typeof(CornerRadius),
            typeof(CompactCheckBox),
            new PropertyMetadata(new CornerRadius(4)));

    public CornerRadius BoxCornerRadius
    {
        get => (CornerRadius)GetValue(BoxCornerRadiusProperty);
        set => SetValue(BoxCornerRadiusProperty, value);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    public event RoutedEventHandler? IsCheckedChanged;

    public event RoutedEventHandler? Click;

    // ── Interaction ───────────────────────────────────────────────────────────

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsEnabled) return;
        _isPressed = true;
        UpdateVisualState();
        if (SuppressToggle)
        {
            Click?.Invoke(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        ToggleState();
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPressed = false;
        UpdateVisualState();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!IsEnabled) return;
        if (e.Key != global::Windows.System.VirtualKey.Space) return;

        if (SuppressToggle)
        {
            Click?.Invoke(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        ToggleState();
        e.Handled = true;
    }

    private void ToggleState()
    {
        if (IsThreeState)
        {
            IsChecked = IsChecked switch
            {
                null => true,
                true => false,
                _ => null,
            };
        }
        else
        {
            IsChecked = IsChecked != true;
        }
    }

    private static void OnIsCheckedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CompactCheckBox box) return;
        box.IsCheckedChanged?.Invoke(box, new RoutedEventArgs());
        box.UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        var accent = GetAccentBrush();
        var border = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];
        var hoverBorder = (Brush)Application.Current.Resources["ControlStrokeColorSecondaryBrush"];
        var checkForeground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];
        var disabledBorder = (Brush)Application.Current.Resources["ControlStrongStrokeColorDisabledBrush"];
        var disabledBg = (Brush)Application.Current.Resources["ControlFillColorDisabledBrush"];

        CheckIcon.Visibility = Visibility.Collapsed;
        IndeterminateBar.Visibility = Visibility.Collapsed;

        if (!IsEnabled)
        {
            BoxBorder.BorderThickness = new Thickness(1);
            BoxBorder.Background = disabledBg;
            BoxBorder.BorderBrush = disabledBorder;
            return;
        }

        switch (IsChecked)
        {
            case true:
                BoxBorder.BorderThickness = new Thickness(0);
                BoxBorder.Background = accent;
                BoxBorder.BorderBrush = accent;
                CheckIcon.Foreground = checkForeground;
                CheckIcon.Visibility = Visibility.Visible;
                break;

            case null:
                BoxBorder.BorderThickness = new Thickness(0);
                BoxBorder.Background = accent;
                BoxBorder.BorderBrush = accent;
                IndeterminateBar.Background = checkForeground;
                IndeterminateBar.Visibility = Visibility.Visible;
                break;

            default:
                BoxBorder.BorderThickness = new Thickness(1);
                BoxBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                BoxBorder.BorderBrush = _isPointerOver ? hoverBorder : border;
                break;
        }
    }

    private Brush GetAccentBrush()
    {
        var key = _isPressed
            ? "AccentFillColorTertiaryBrush"
            : _isPointerOver
                ? "AccentFillColorSecondaryBrush"
                : "AccentFillColorDefaultBrush";
        return (Brush)Application.Current.Resources[key];
    }
}
