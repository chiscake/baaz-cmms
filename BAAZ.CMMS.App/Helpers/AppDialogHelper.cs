using System;
using System.Threading.Tasks;

using DevWinUI;

using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Подтверждающие диалоги DevWinUI (оконный MessageBox на базе UacStyleDialogView).</summary>
public static class AppDialogHelper
{
    /// <summary>
    /// Диалог с многострочным полем ввода — для комментариев к действиям workflow
    /// заявки (отклонение, передача в другой отдел, смена зоны и т.п.).
    /// Возвращает <c>null</c> при отмене или если <paramref name="required"/> и поле пустое.
    /// </summary>
    public static async Task<string?> PromptTextAsync(
        string title,
        string placeholder,
        Window? owner = null,
        bool required = false)
    {
        var textBox = new Microsoft.UI.Xaml.Controls.TextBox
        {
            PlaceholderText = placeholder,
            AcceptsReturn = true,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            Height = 80,
        };

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = title,
            Content = textBox,
            PrimaryButtonText = ResourceStrings.Get("Common_Ok"),
            CloseButtonText = ResourceStrings.Get("Common_Cancel"),
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            XamlRoot = (owner ?? App.MainWindow)?.Content.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            return null;

        var text = textBox.Text?.Trim();
        return required && string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    /// Показать предупреждающий диалог OK/Cancel. По умолчанию фокус на «Отмена».
    /// </summary>
    public static Task<bool> ConfirmAsync(
        string title,
        string message,
        Window? owner = null,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button2)
        => ConfirmAsync(title, message, owner, defaultButton, MessageBoxIcon.Warning);

    /// <summary>
    /// Диалог OK/Cancel с выбранной иконкой.
    /// </summary>
    public static async Task<bool> ConfirmAsync(
        string title,
        string message,
        Window? owner,
        MessageBoxDefaultButton defaultButton,
        MessageBoxIcon icon)
    {
        var result = await new MessageBox
        {
            Owner = owner ?? App.MainWindow,
            Title = title,
            Message = message,
            Icon = icon,
            Buttons = MessageBoxButtons.OKCancel,
            DefaultButton = defaultButton,
        }.ShowAsync();

        return result == MessageBoxResult.OK;
    }

    /// <summary>
    /// Диалог успеха OK/Cancel (зелёная иконка). По умолчанию фокус на «OK».
    /// </summary>
    public static Task<bool> ConfirmSuccessAsync(
        string title,
        string message,
        Window? owner = null)
        => ConfirmAsync(
            title,
            message,
            owner,
            MessageBoxDefaultButton.Button1,
            MessageBoxIcon.Success);
}
