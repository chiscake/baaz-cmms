using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.MaintenanceTypePicker;

public sealed partial class MaintenanceTypePickerCard : UserControl
{
    private bool _isPointerOver;
    private bool _suppressRadioCallback;

    public MaintenanceTypePickerCard()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyVisualState();
    }

    public event EventHandler<string>? SelectionRequested;

    public static readonly DependencyProperty ItemKeyProperty =
        DependencyProperty.Register(
            nameof(ItemKey),
            typeof(string),
            typeof(MaintenanceTypePickerCard),
            new PropertyMetadata(string.Empty, OnSelectionVisualChanged));

    public string ItemKey
    {
        get => (string)GetValue(ItemKeyProperty);
        set => SetValue(ItemKeyProperty, value);
    }

    public static readonly DependencyProperty SelectedKeyProperty =
        DependencyProperty.Register(
            nameof(SelectedKey),
            typeof(string),
            typeof(MaintenanceTypePickerCard),
            new PropertyMetadata(string.Empty, OnSelectionVisualChanged));

    public string SelectedKey
    {
        get => (string)GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(MaintenanceTypePickerCard),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(MaintenanceTypePickerCard),
            new PropertyMetadata(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty RadioGroupNameProperty =
        DependencyProperty.Register(
            nameof(RadioGroupName),
            typeof(string),
            typeof(MaintenanceTypePickerCard),
            new PropertyMetadata(string.Empty));

    public string RadioGroupName
    {
        get => (string)GetValue(RadioGroupNameProperty);
        set => SetValue(RadioGroupNameProperty, value);
    }

    private static void OnSelectionVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MaintenanceTypePickerCard card)
            card.ApplyVisualState();
    }

    private void OnRadioChecked(object sender, RoutedEventArgs e)
    {
        if (_suppressRadioCallback || SelectRadio.IsChecked != true)
            return;

        SelectionRequested?.Invoke(this, ItemKey);
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        ApplyVisualState();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        ApplyVisualState();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsEnabled)
            return;

        SelectionRequested?.Invoke(this, ItemKey);
        e.Handled = true;
    }

    private void ApplyVisualState()
    {
        var isSelected = !string.IsNullOrEmpty(ItemKey)
            && string.Equals(ItemKey, SelectedKey, StringComparison.Ordinal);

        _suppressRadioCallback = true;
        SelectRadio.IsChecked = isSelected;
        _suppressRadioCallback = false;

        CardBorder.BorderThickness = new Thickness(isSelected ? 2 : 1);
        CardBorder.BorderBrush = isSelected
            ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            : (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];

        if (isSelected)
        {
            CardBorder.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            return;
        }

        CardBorder.Background = _isPointerOver
            ? (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"]
            : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
    }
}
