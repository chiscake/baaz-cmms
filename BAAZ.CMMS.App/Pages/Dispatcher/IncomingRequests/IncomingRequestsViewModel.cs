using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;
using BAAZ.CMMS.App.Services.Notifications;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;

public sealed partial class IncomingRequestsViewModel : PageViewModelBase
{
    private static readonly string[] ActiveStatuses = ["accepted", "in_progress"];
    private static readonly string[] CompletedStatuses = ["completed"];

    private readonly IRequestService _requestService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly INavBadgeService _navBadgeService;

    private readonly IncomingRequestsColumn _newColumn;
    private readonly IncomingRequestsColumn _acceptedColumn;
    private readonly IncomingRequestsColumn _inProgressColumn;
    private readonly IncomingRequestsColumn _completedColumn;

    private bool _realtimeSubscribed;

    public IncomingRequestsViewModel(
        IRequestService requestService,
        IAuthService authService,
        INavigationService navigationService,
        IRealtimeNotificationService realtimeService,
        INavBadgeService navBadgeService)
    {
        _requestService = requestService;
        _authService = authService;
        _navigationService = navigationService;
        _realtimeService = realtimeService;
        _navBadgeService = navBadgeService;

        _newColumn = CreateColumn("new", "IncomingRequests_Tab_New");
        _acceptedColumn = CreateColumn("accepted", "IncomingRequests_Column_Accepted");
        _inProgressColumn = CreateColumn("in_progress", "IncomingRequests_Column_InProgress");
        _completedColumn = CreateColumn("completed", "IncomingRequests_Column_Completed");

        Columns = [_newColumn, _acceptedColumn, _inProgressColumn, _completedColumn];
        foreach (var column in Columns)
            column.RefreshHeader();
    }

    public IReadOnlyList<IncomingRequestsColumn> Columns { get; }

    public IncomingRequestsColumn NewColumn => _newColumn;

    public IncomingRequestsColumn AcceptedColumn => _acceptedColumn;

    public IncomingRequestsColumn InProgressColumn => _inProgressColumn;

    public IncomingRequestsColumn CompletedColumn => _completedColumn;

    public override string PageTitle => ResourceStrings.Get("Nav_IncomingRequests");

    public string ActionAccept => ResourceStrings.Get("IncomingRequests_Action_Accept");
    public string ActionReject => ResourceStrings.Get("IncomingRequests_Action_Reject");
    public string ActionOpen => ResourceStrings.Get("IncomingRequests_Action_Open");

    public string ColumnEmptyText => ResourceStrings.Get("IncomingRequests_Column_Empty");

    public string ColumnCountBadgeBackgroundKey => StatusBadgeFactory.NeutralBadgeBackgroundKey;

    public string ColumnCountBadgeForegroundKey => StatusBadgeFactory.NeutralBadgeForegroundKey;

    public bool IsAdmin => _authService.CurrentProfile?.Role == UserRole.Admin;

    public Guid? OwnRepairDepartmentId => _authService.CurrentProfile?.RepairDepartmentId;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsActionBusy { get; set; }

    public async Task OnPageLoadedAsync()
    {
        SubscribeRealtime();
        await LoadAsync();
    }

    public void UnsubscribeRealtime()
    {
        if (!_realtimeSubscribed)
            return;

        _realtimeService.EventReceived -= OnRealtimeEvent;
        _realtimeSubscribed = false;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenRequest(IncomingRequestRow? row)
    {
        if (row is null)
            return;

        _navigationService.NavigateTo("RequestDetail", new RequestDetailNavigationArgs { RequestId = row.Id });
    }

    [RelayCommand]
    private async Task AcceptAsync(IncomingRequestRow? row)
    {
        if (row is null || IsActionBusy)
            return;

        IsActionBusy = true;
        try
        {
            var result = await _requestService.AcceptRequestAsync(row.Id);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("IncomingRequests_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("IncomingRequests_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private async Task RejectAsync(IncomingRequestRow? row)
    {
        if (row is null || IsActionBusy)
            return;

        var comment = await AppDialogHelper.PromptTextAsync(
            ResourceStrings.Get("IncomingRequests_Confirm_Reject_Title"),
            ResourceStrings.Get("IncomingRequests_Confirm_Reject_Placeholder"),
            App.MainWindow,
            required: true);

        if (comment is null)
            return;

        var actorId = _authService.CurrentProfile?.Id ?? Guid.Empty;

        IsActionBusy = true;
        try
        {
            var result = await _requestService.RejectRequestAsync(row.Id, actorId, comment);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("IncomingRequests_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("IncomingRequests_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        InfoBanner.Report(string.Empty);
        try
        {
            var newTask = _requestService.GetIncomingRequestsAsync();
            var activeTask = _requestService.GetRequestsByStatusesAsync(ActiveStatuses);
            var completedTask = _requestService.GetRequestsByStatusesAsync(CompletedStatuses);
            await Task.WhenAll(newTask, activeTask, completedTask);

            PopulateColumn(_newColumn, newTask.Result);
            PopulateColumn(_acceptedColumn, activeTask.Result.Where(i => i.Status == "accepted"));
            PopulateColumn(_inProgressColumn, activeTask.Result.Where(i => i.Status == "in_progress"));
            PopulateColumn(_completedColumn, completedTask.Result);
            SyncNavBadgeFromNewColumn();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulateColumn(IncomingRequestsColumn column, IEnumerable<RequestListItem> items)
    {
        column.Rows.Clear();
        foreach (var item in RequestListSortHelper.SortForIncomingKanban(items))
            column.Rows.Add(IncomingRequestRow.FromListItem(item, this));

        column.RefreshHeader();
    }

    private void SyncNavBadgeFromNewColumn()
    {
        _navBadgeService.SetCount(NavItemIds.DispatcherIncomingRequests, _newColumn.Rows.Count);
    }

    private void SubscribeRealtime()
    {
        if (_realtimeSubscribed)
            return;

        _realtimeService.EventReceived += OnRealtimeEvent;
        _realtimeSubscribed = true;
    }

    private void OnRealtimeEvent(object? sender, RealtimeEvent e)
    {
        if (!string.Equals(e.Table, "requests", StringComparison.Ordinal))
            return;

        RealtimeUiRefresh.Enqueue(LoadAsync);
    }

    private static IncomingRequestsColumn CreateColumn(string statusKey, string baseLabelResourceKey)
    {
        var marker = StatusMarkerFactory.ForRequestStatus(statusKey);
        return new IncomingRequestsColumn
        {
            StatusKey = statusKey,
            BaseLabel = ResourceStrings.Get(baseLabelResourceKey),
            MarkerColorKey = marker.ColorKey,
            MarkerTooltip = marker.Tooltip,
        };
    }
}
