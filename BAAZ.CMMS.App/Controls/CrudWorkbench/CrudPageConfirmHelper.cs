using System;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Общие confirm-диалоги для catalog-страниц CrudWorkbench.</summary>
public static class CrudPageConfirmHelper
{
    public static async Task<bool> ConfirmDeleteRowAsync(
        string titleKey,
        string messageFormatKey,
        string displayName,
        Window? owner = null)
    {
        var title = ResourceStrings.Get(titleKey);
        var message = string.Format(ResourceStrings.Get(messageFormatKey), displayName);
        return await AppDialogHelper.ConfirmAsync(title, message, owner ?? App.MainWindow);
    }

    public static async Task<bool> ConfirmBulkDeleteAsync(
        string resourcePrefix,
        int count,
        Window? owner = null)
    {
        var title = ResourceStrings.Get($"{resourcePrefix}_DeleteBulk_Title");
        var message = string.Format(ResourceStrings.Get($"{resourcePrefix}_DeleteBulk_Message"), count);
        return await AppDialogHelper.ConfirmAsync(title, message, owner ?? App.MainWindow);
    }

    public static async Task<bool> ConfirmArchiveRowAsync(
        bool archiving,
        string archiveTitleKey,
        string archiveMessageKey,
        string restoreTitleKey,
        string restoreMessageKey,
        string displayName,
        Window? owner = null)
    {
        var title = ResourceStrings.Get(archiving ? archiveTitleKey : restoreTitleKey);
        var message = string.Format(
            ResourceStrings.Get(archiving ? archiveMessageKey : restoreMessageKey),
            displayName);
        return await AppDialogHelper.ConfirmAsync(title, message, owner ?? App.MainWindow);
    }

    public static async Task<bool> ConfirmBulkArchiveAsync(
        string archiveBulkTitleKey,
        string archiveBulkMessageKey,
        string restoreBulkTitleKey,
        string restoreBulkMessageKey,
        int selectedCount,
        bool anyActive,
        bool anyInactive,
        Window? owner = null)
    {
        string titleKey;
        string messageKey;
        if (anyActive && !anyInactive)
        {
            titleKey = archiveBulkTitleKey;
            messageKey = archiveBulkMessageKey;
        }
        else if (anyInactive && !anyActive)
        {
            titleKey = restoreBulkTitleKey;
            messageKey = restoreBulkMessageKey;
        }
        else
        {
            titleKey = archiveBulkTitleKey;
            messageKey = archiveBulkMessageKey;
        }

        var title = ResourceStrings.Get(titleKey);
        var message = string.Format(ResourceStrings.Get(messageKey), selectedCount);
        return await AppDialogHelper.ConfirmAsync(title, message, owner ?? App.MainWindow);
    }
}
