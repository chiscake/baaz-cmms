using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.TmsIssuance;

using Microsoft.UI.Xaml;

using WinRT.Interop;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Services.Notifications;

public sealed class ShellNotificationPresenter : IShellNotificationPresenter
{
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IWindowsToastService _toastService;
    private readonly INavBadgeService _navBadgeService;
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService;
    private readonly IWindowProvider _windowProvider;

    private readonly HashSet<Guid> _toastedRecordIds = [];
    private readonly HashSet<string> _toolRequisitionToastKeys = [];
    private readonly HashSet<long> _scheduleToastMinuteBuckets = [];
    private bool _started;

    public ShellNotificationPresenter(
        IRealtimeNotificationService realtimeService,
        IAuthService authService,
        INavigationService navigationService,
        IWindowsToastService toastService,
        INavBadgeService navBadgeService,
        IRequestService requestService,
        IMaintenanceService maintenanceService,
        ITmsToolRequisitionService tmsToolRequisitionService,
        IWindowProvider windowProvider)
    {
        _realtimeService = realtimeService;
        _authService = authService;
        _navigationService = navigationService;
        _toastService = toastService;
        _navBadgeService = navBadgeService;
        _requestService = requestService;
        _maintenanceService = maintenanceService;
        _tmsToolRequisitionService = tmsToolRequisitionService;
        _windowProvider = windowProvider;
    }

    public void Start()
    {
        if (_started)
            return;

        _realtimeService.EventReceived += OnRealtimeEvent;
        _started = true;
        _ = SyncInitialBadgesAsync();
    }

    public void Stop()
    {
        if (!_started)
            return;

        _realtimeService.EventReceived -= OnRealtimeEvent;
        _toastedRecordIds.Clear();
        _scheduleToastMinuteBuckets.Clear();
        _toolRequisitionToastKeys.Clear();
        _started = false;
    }

    public async Task SyncInitialBadgesAsync()
    {
        var profile = _authService.CurrentProfile;
        if (profile is null)
            return;

        if (profile.Role is UserRole.Dispatcher or UserRole.Admin)
        {
            try
            {
                var incoming = await _requestService.GetIncomingRequestsAsync();
                var newCount = incoming.Count(r => string.Equals(r.Status, "new", StringComparison.Ordinal));
                _navBadgeService.SetCount(NavItemIds.DispatcherIncomingRequests, newCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellNotify] Initial badge sync failed: {ex.Message}");
            }

            try
            {
                var scheduleCount = await _maintenanceService.GetScheduleNavBadgeCountAsync();
                _navBadgeService.SetCount(NavItemIds.DispatcherMaintenanceSchedule, scheduleCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellNotify] Initial schedule badge sync failed: {ex.Message}");
            }

            try
            {
                await SyncToolRequisitionBadgeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellNotify] Initial tool requisition badge sync failed: {ex.Message}");
            }
        }
    }

    public void NavigateFromToast(string? pageKey, Guid? requestId, Guid? linkId = null)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
            return;

        object? parameter = pageKey switch
        {
            "RequestDetail" when requestId is { } id
                => new RequestDetailNavigationArgs { RequestId = id },
            "MyRequests" when requestId is { } myId
                => new MyRequestsNavigationArgs(myId),
            "ToolRequisitionHistory" when linkId is { } toolLinkId
                => new ToolRequisitionHistoryNavigationArgs { LinkId = toolLinkId },
            _ => null,
        };

        _navigationService.NavigateTo(pageKey, parameter);
    }

    private void OnRealtimeEvent(object? sender, RealtimeEvent e)
    {
        try
        {
            switch (e.Table)
            {
                case "requests":
                    HandleRequestEvent(e);
                    break;
                case "maintenance_schedule":
                    HandleScheduleEvent(e);
                    break;
                case "tms_tool_requisition_links":
                    HandleToolRequisitionLinkEvent(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ShellNotify] OnRealtimeEvent error: {ex.Message}");
        }
    }

    private void HandleRequestEvent(RealtimeEvent e)
    {
        var profile = _authService.CurrentProfile;
        if (profile is null)
            return;

        var request = e.Payload as RequestModel;
        var requestId = e.RecordId ?? request?.Id;
        if (requestId is not Guid requestGuid || requestGuid == Guid.Empty)
            return;

        var status = request?.Status ?? string.Empty;
        var requestNumber = request?.RequestNumber ?? requestGuid.ToString("D");

        if (e.EventType == RealtimeEventType.Insert
            && string.Equals(status, "new", StringComparison.Ordinal)
            && profile.Role is UserRole.Dispatcher or UserRole.Admin)
        {
            if (ShouldShowToast(
                    suppressWhenFocusedOnPage: "IncomingRequests",
                    recordId: requestGuid,
                    pageKey: "IncomingRequests"))
            {
                TryShowToast(requestGuid, () =>
                    _toastService.ShowRequestNew(requestGuid, requestNumber));
            }
        }
        else if (e.EventType == RealtimeEventType.Update
                 && request is not null
                 && request.RequesterId == profile.Id)
        {
            _navBadgeService.Increment(NavItemIds.RequesterMyRequests);

            if (ShouldShowToast(
                    suppressWhenFocusedOnPage: "MyRequests",
                    recordId: requestGuid,
                    pageKey: "MyRequests",
                    sameRecordOnPage: true))
            {
                TryShowToast(requestGuid, () =>
                    _toastService.ShowRequestStatusChanged(
                        requestGuid,
                        requestNumber,
                        RequestStatusHelper.GetLabel(status)));
            }
        }

        if (profile.Role is UserRole.Dispatcher or UserRole.Admin)
            _ = SyncIncomingBadgeAsync();
    }

    private void HandleScheduleEvent(RealtimeEvent e)
    {
        var profile = _authService.CurrentProfile;
        if (profile is null || profile.Role is not (UserRole.Dispatcher or UserRole.Admin))
            return;

        var onSchedulePage = string.Equals(
            _navigationService.CurrentPageKey,
            "MaintenanceSchedule",
            StringComparison.Ordinal);
        if (!onSchedulePage)
            RealtimeUiRefresh.EnqueueDebounced("schedule-nav-badge", SyncScheduleBadgeAsync);

        if (e.EventType != RealtimeEventType.Insert)
            return;

        if (e.Payload is not MaintenanceScheduleModel schedule || !schedule.NotifyDispatchers)
            return;

        if (!ShouldShowToast(
                suppressWhenFocusedOnPage: "MaintenanceSchedule",
                recordId: null,
                pageKey: "MaintenanceSchedule"))
            return;

        if (!TryAcquireScheduleToastBucket())
            return;

        _toastService.ShowScheduleUpdated();
    }

    private bool TryAcquireScheduleToastBucket()
    {
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        if (!_scheduleToastMinuteBuckets.Add(bucket))
            return false;

        if (_scheduleToastMinuteBuckets.Count > 1000)
            _scheduleToastMinuteBuckets.Clear();

        return true;
    }

    private void HandleToolRequisitionLinkEvent(RealtimeEvent e)
    {
        var profile = _authService.CurrentProfile;
        if (profile is null || profile.Role is not (UserRole.Dispatcher or UserRole.Admin))
            return;

        if (e.EventType != RealtimeEventType.Update)
            return;

        RealtimeUiRefresh.EnqueueDebounced("tool-requisition-nav-badge", SyncToolRequisitionBadgeAsync);
        RealtimeUiRefresh.Enqueue(() => HandleToolRequisitionToastAsync(e));
    }

    private async Task HandleToolRequisitionToastAsync(RealtimeEvent e)
    {
        var link = e.Payload as TmsToolRequisitionLinkModel;
        if (link is null && e.RecordId is Guid linkId)
        {
            var fetched = await _tmsToolRequisitionService.GetLocalByIdAsync(linkId);
            if (fetched.IsSuccess)
                link = fetched.Value;
        }

        if (link is null)
            return;

        var status = link.LastKnownStatus ?? string.Empty;
        if (!string.Equals(status, TmsRequisitionStatuses.PartiallyReserved, StringComparison.Ordinal)
            && !string.Equals(status, TmsRequisitionStatuses.ReadyForIssue, StringComparison.Ordinal))
        {
            return;
        }

        if (!ShouldShowToast(
                suppressWhenFocusedOnPage: "ToolRequisitionHistory",
                recordId: link.Id,
                pageKey: "ToolRequisitionHistory"))
            return;

        if (!TryAcquireToolRequisitionToast(link.Id, status))
            return;

        var number = TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId);
        var statusLabel = ToolRequisitionLabels.FormatTmsStatus(status);
        _toastService.ShowToolRequisitionReady(link.Id, number, statusLabel);
    }

    private bool TryAcquireToolRequisitionToast(Guid linkId, string status)
    {
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        var key = $"{linkId}|{status}|{bucket}";
        if (!_toolRequisitionToastKeys.Add(key))
            return false;

        if (_toolRequisitionToastKeys.Count > 1000)
            _toolRequisitionToastKeys.Clear();

        return true;
    }

    private async Task SyncIncomingBadgeAsync()
    {
        try
        {
            var incoming = await _requestService.GetIncomingRequestsAsync();
            var newCount = incoming.Count(r => string.Equals(r.Status, "new", StringComparison.Ordinal));
            _navBadgeService.SetCount(NavItemIds.DispatcherIncomingRequests, newCount);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ShellNotify] SyncIncomingBadge failed: {ex.Message}");
        }
    }

    private async Task SyncScheduleBadgeAsync()
    {
        try
        {
            var count = await _maintenanceService.GetScheduleNavBadgeCountAsync();
            _navBadgeService.SetCount(NavItemIds.DispatcherMaintenanceSchedule, count);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ShellNotify] SyncScheduleBadge failed: {ex.Message}");
        }
    }

    private async Task SyncToolRequisitionBadgeAsync()
    {
        try
        {
            var links = await _tmsToolRequisitionService.ListAllLocalAsync();
            var readyCount = links.IsSuccess && links.Value is not null
                ? links.Value.Count(l =>
                    string.Equals(l.LastKnownStatus, TmsRequisitionStatuses.ReadyForIssue, StringComparison.Ordinal))
                : 0;
            _navBadgeService.SetCount(NavItemIds.DispatcherToolRequisitionHistory, readyCount);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ShellNotify] SyncToolRequisitionBadge failed: {ex.Message}");
        }
    }

    private bool ShouldShowToast(
        string suppressWhenFocusedOnPage,
        Guid? recordId,
        string pageKey,
        bool sameRecordOnPage = false)
    {
        if (!IsMainWindowFocused())
            return true;

        if (!string.Equals(_navigationService.CurrentPageKey, suppressWhenFocusedOnPage, StringComparison.Ordinal))
            return true;

        if (!sameRecordOnPage || recordId is null)
            return false;

        return !IsViewingSameRecord(pageKey, recordId.Value);
    }

    private bool IsViewingSameRecord(string pageKey, Guid recordId)
    {
        if (!string.Equals(_navigationService.CurrentPageKey, pageKey, StringComparison.Ordinal))
            return false;

        // Для deep-link страниц совпадение record проверяется по текущему URL-параметру навигации —
        // упрощённо: если уже на RequestDetail/MyRequests в фокусе, не дублируем toast.
        return pageKey is "RequestDetail" or "MyRequests";
    }

    private void TryShowToast(Guid dedupId, Action show)
    {
        if (!_toastedRecordIds.Add(dedupId))
            return;

        if (_toastedRecordIds.Count > 1000)
            _toastedRecordIds.Clear();

        show();
    }

    private bool IsMainWindowFocused()
    {
        try
        {
            var window = _windowProvider.GetMainWindow();
            var hwnd = WindowNative.GetWindowHandle(window);
            return GetForegroundWindow() == hwnd;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
