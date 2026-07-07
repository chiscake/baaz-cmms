using System;
using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudEditorPanel : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Инверсия <see cref="IsBusy"/> — x:Bind не поддерживает оператор ! в этой версии WinUI.</summary>
    public bool IsNotBusy => !IsBusy;

    public CrudEditorPanel()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(CrudEditorPanel), new PropertyMetadata(string.Empty));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public static readonly DependencyProperty SaveLabelProperty =
        DependencyProperty.Register(nameof(SaveLabel), typeof(string),
            typeof(CrudEditorPanel), new PropertyMetadata(string.Empty));
    public string SaveLabel { get => (string)GetValue(SaveLabelProperty); set => SetValue(SaveLabelProperty, value); }

    public static readonly DependencyProperty CancelLabelProperty =
        DependencyProperty.Register(nameof(CancelLabel), typeof(string),
            typeof(CrudEditorPanel), new PropertyMetadata(string.Empty));
    public string CancelLabel { get => (string)GetValue(CancelLabelProperty); set => SetValue(CancelLabelProperty, value); }

    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool),
            typeof(CrudEditorPanel), new PropertyMetadata(false, OnIsBusyChanged));
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CrudEditorPanel panel)
            panel.PropertyChanged?.Invoke(panel, new PropertyChangedEventArgs(nameof(IsNotBusy)));
    }

    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.Register(nameof(HasError), typeof(bool),
            typeof(CrudEditorPanel), new PropertyMetadata(false));
    public bool HasError { get => (bool)GetValue(HasErrorProperty); set => SetValue(HasErrorProperty, value); }

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string),
            typeof(CrudEditorPanel), new PropertyMetadata(string.Empty));
    public string ErrorMessage { get => (string)GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }

    public static readonly DependencyProperty FormContentProperty =
        DependencyProperty.Register(nameof(FormContent), typeof(object),
            typeof(CrudEditorPanel), new PropertyMetadata(null));
    public object? FormContent { get => GetValue(FormContentProperty); set => SetValue(FormContentProperty, value); }

    public event EventHandler<EventArgs>? SaveClicked;
    public event EventHandler<EventArgs>? CancelClicked;

    private void SaveBtn_Click(object sender, RoutedEventArgs e) => SaveClicked?.Invoke(this, EventArgs.Empty);
    private void CancelBtn_Click(object sender, RoutedEventArgs e) => CancelClicked?.Invoke(this, EventArgs.Empty);
    private void CloseButton_Click(object sender, RoutedEventArgs e) => CancelClicked?.Invoke(this, EventArgs.Empty);
}
