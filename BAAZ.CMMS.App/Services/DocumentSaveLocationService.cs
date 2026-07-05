using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BAAZ.CMMS.App.Localization;
using Helpers.Settings;
using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Services;

public sealed class DocumentSaveLocationService(IFilePickerService filePicker) : IDocumentSaveLocationService
{
    private readonly IFilePickerService _filePicker = filePicker;

    public string? LastSaveDirectory
    {
        get
        {
            var path = SettingsHelper.Current.LastDocumentSaveDirectory;
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
    }

    public async Task<string?> PickDocxSavePathAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var initialDir = Directory.Exists(LastSaveDirectory) ? LastSaveDirectory : null;

        var path = await _filePicker.PickSaveFileAsync(
            suggestedFileName,
            new Dictionary<string, IReadOnlyList<string>>
            {
                [ResourceStrings.Get("DocumentSave_FileTypeLabel")] = [".docx"],
            },
            ".docx",
            ResourceStrings.Get("DocumentSave_PickerTitle"),
            initialDir);

        if (path is not null)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                SettingsHelper.Current.LastDocumentSaveDirectory = directory;
        }

        return path;
    }
}
