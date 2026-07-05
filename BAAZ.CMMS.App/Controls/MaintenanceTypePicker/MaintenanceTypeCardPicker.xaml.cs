using System;
using System.Collections.Generic;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.MaintenanceTypePicker;

/// <summary>Карточный выбор вида ТО: коллекция элементов + выбранный ключ.</summary>
public sealed partial class MaintenanceTypeCardPicker : UserControl
{
    public MaintenanceTypeCardPicker()
    {
        InitializeComponent();
        RadioGroupName = Guid.NewGuid().ToString("N");
    }

    public string RadioGroupName { get; }

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(IList<MaintenanceTypePickerItem>),
            typeof(MaintenanceTypeCardPicker),
            new PropertyMetadata(null));

    public IList<MaintenanceTypePickerItem>? Items
    {
        get => (IList<MaintenanceTypePickerItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly DependencyProperty SelectedKeyProperty =
        DependencyProperty.Register(
            nameof(SelectedKey),
            typeof(string),
            typeof(MaintenanceTypeCardPicker),
            new PropertyMetadata(string.Empty, OnSelectedKeyChanged));

    public string SelectedKey
    {
        get => (string)GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    public event EventHandler<string>? SelectedKeyChanged;

    private static void OnSelectedKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MaintenanceTypeCardPicker picker)
            picker.SelectedKeyChanged?.Invoke(picker, picker.SelectedKey);
    }

    private void OnCardSelectionRequested(object sender, string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        SelectedKey = key;
    }
}
