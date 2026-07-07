using System;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using CommunityToolkit.Mvvm.ComponentModel;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Admin.AuditLog;

public sealed partial class AuditLogViewModel : PageViewModelBase
{
    public AuditLogViewModel(AuditLogTableViewModel tableViewModel)
    {
        TableViewModel = tableViewModel;
        TableViewModel.RecordPicked += OnRecordPicked;
    }

    public AuditLogTableViewModel TableViewModel { get; }

    public override string PageTitle => ResourceStrings.Get("Nav_AuditLog");

    public string DetailLabelActor => ResourceStrings.Get("AuditLog_Detail_Actor");
    public string DetailLabelChangedAt => ResourceStrings.Get("AuditLog_Detail_ChangedAt");
    public string DetailLabelRecordKey => ResourceStrings.Get("AuditLog_Detail_RecordKey");
    public string DetailLabelChanges => ResourceStrings.Get("AuditLog_Detail_Changes");
    public string EmptySelectionText => ResourceStrings.Get("AuditLog_Empty_Selection");

    public bool ShowEmptySelection => !HasDetail;

    [ObservableProperty]
    public partial bool HasDetail { get; set; }

    partial void OnHasDetailChanged(bool value) => OnPropertyChanged(nameof(ShowEmptySelection));

    [ObservableProperty]
    public partial string DetailTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailActor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailChangedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailRecordKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailChangesJson { get; set; } = string.Empty;

    public async Task OnPageLoadedAsync(object? parameter = null)
        => await TableViewModel.OnPageLoadedAsync();

    private void OnRecordPicked(object? sender, AuditLogRow row)
        => LoadDetail(row);

    private void LoadDetail(AuditLogRow row)
    {
        var operationLabel = AuditLogOperationHelper.GetLabel(row.Operation);
        DetailTitle = $"{operationLabel} · {row.TableName}";
        DetailActor = row.ActorName;
        DetailChangedAt = DateTimeDisplayHelper.Format(row.ChangedAt);
        DetailRecordKey = row.RecordKey;
        DetailChangesJson = BuildChangesJson(row);
        HasDetail = true;
    }

    private static string BuildChangesJson(AuditLogRow row)
    {
        return row.Operation switch
        {
            "INSERT" => row.NewDataJson ?? string.Empty,
            "DELETE" => row.OldDataJson ?? string.Empty,
            "UPDATE" => string.Join(
                Environment.NewLine + Environment.NewLine,
                new[]
                {
                    row.OldDataJson is { Length: > 0 } old
                        ? $"{ResourceStrings.Get("AuditLog_Detail_OldData")}:{Environment.NewLine}{old}"
                        : null,
                    row.NewDataJson is { Length: > 0 } @new
                        ? $"{ResourceStrings.Get("AuditLog_Detail_NewData")}:{Environment.NewLine}{@new}"
                        : null,
                }.Where(static part => part is not null)),
            _ => string.Join(
                Environment.NewLine + Environment.NewLine,
                new[] { row.OldDataJson, row.NewDataJson }.Where(static part => !string.IsNullOrWhiteSpace(part))),
        };
    }
}
