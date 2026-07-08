using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Home.Dashboards;
using BAAZ.CMMS.Core.Realtime;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Home.AdminHome;

public sealed class AdminHomeViewModel : PageViewModelBase
{
    private readonly IRealtimeNotificationService _realtimeService;
    private bool _realtimeSubscribed;

    public AdminHomeViewModel(
        AdminHomeDashboardViewModel adminSection,
        DispatcherHomeDashboardViewModel dispatcherSection,
        RequesterHomeDashboardViewModel requesterSection,
        IRealtimeNotificationService realtimeService)
    {
        AdminSection = adminSection;
        DispatcherSection = dispatcherSection;
        RequesterSection = requesterSection;
        _realtimeService = realtimeService;
    }

    public override string PageTitle => ResourceStrings.Get("Home_Title");

    public AdminHomeDashboardViewModel AdminSection { get; }

    public DispatcherHomeDashboardViewModel DispatcherSection { get; }

    public RequesterHomeDashboardViewModel RequesterSection { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        InfoBanner.Report(string.Empty);

        await Task.WhenAll(
            AdminSection.LoadAsync(cancellationToken),
            DispatcherSection.LoadAsync(cancellationToken),
            RequesterSection.LoadAsync(cancellationToken));

        ReportSectionErrors(AdminSection, DispatcherSection, RequesterSection);
    }

    private void ReportSectionErrors(params HomeDashboardSectionViewModel[] sections)
    {
        if (sections.Any(s => s.LoadError is not null))
        {
            InfoBanner.Report(ResourceStrings.Get("Home_LoadError"), InfoBarSeverity.Error);
        }
    }

    public void SubscribeRealtime()
    {
        if (_realtimeSubscribed)
            return;

        _realtimeService.EventReceived += OnRealtimeEvent;
        _realtimeSubscribed = true;
    }

    public void UnsubscribeRealtime()
    {
        if (!_realtimeSubscribed)
            return;

        _realtimeService.EventReceived -= OnRealtimeEvent;
        _realtimeSubscribed = false;
    }

    private void OnRealtimeEvent(object? sender, RealtimeEvent e)
    {
        if (e.Table is not "tms_tool_requisition_links")
            return;

        RealtimeUiRefresh.EnqueueDebounced(
            "admin-home-tool-requisitions",
            () => DispatcherSection.LoadAsync());
    }
}
