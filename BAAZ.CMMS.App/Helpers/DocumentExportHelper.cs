using System;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Services;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.DocumentExport;

using DevWinUI;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Helpers;

public static class DocumentExportHelper
{
    public static async Task<bool> RunDocxExportAsync(
        PageViewModelBase page,
        IDocumentSaveLocationService saveLocationService,
        IWindowsShellFileService shellFileService,
        string suggestedFileName,
        string successTitleResourceKey,
        string successMessageResourceKey,
        Func<string, Task<DataResult<DocumentExportResult>>> exportAsync)
    {
        var targetPath = await saveLocationService.PickDocxSavePathAsync(suggestedFileName);
        if (targetPath is null)
            return false;

        page.InfoBanner.Report(string.Empty);
        var result = await exportAsync(targetPath);
        if (!result.IsSuccess || result.Value is null)
        {
            page.InfoBanner.Report(
                result.Error?.MessageKey ?? ResourceStrings.Get("DocumentExport_Error_SaveFailed"),
                InfoBarSeverity.Error);
            return false;
        }

        page.InfoBanner.Report(
            $"{ResourceStrings.Get(successTitleResourceKey)} {string.Format(ResourceStrings.Get(successMessageResourceKey), result.Value.SavedFilePath)}",
            InfoBarSeverity.Success,
            TimeSpan.FromSeconds(5));

        await shellFileService.OpenFileAsync(result.Value.SavedFilePath);
        return true;
    }

    public static async Task<bool> RunXlsxExportAsync(
        PageViewModelBase page,
        IDocumentSaveLocationService saveLocationService,
        IWindowsShellFileService shellFileService,
        string suggestedFileName,
        string successTitleResourceKey,
        string successMessageResourceKey,
        Func<string, DataResult<DocumentExportResult>> export)
    {
        var targetPath = await saveLocationService.PickXlsxSavePathAsync(suggestedFileName);
        if (targetPath is null)
            return false;

        page.InfoBanner.Report(string.Empty);
        var result = export(targetPath);
        if (!result.IsSuccess || result.Value is null)
        {
            page.InfoBanner.Report(
                result.Error?.MessageKey ?? ResourceStrings.Get("DocumentExport_Error_SaveFailed"),
                InfoBarSeverity.Error);
            return false;
        }

        page.InfoBanner.Report(
            $"{ResourceStrings.Get(successTitleResourceKey)} {string.Format(ResourceStrings.Get(successMessageResourceKey), result.Value.SavedFilePath)}",
            InfoBarSeverity.Success,
            TimeSpan.FromSeconds(5));

        await shellFileService.OpenFileAsync(result.Value.SavedFilePath);
        return true;
    }
}
