using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Helpers.Microsoft;
using Helpers.Settings;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Services;
using BAAZ.CMMS.App.Services.Notifications;
using BAAZ.CMMS.App.Pages;
using BAAZ.CMMS.App.Pages.Admin.AllRequests;
using BAAZ.CMMS.App.Pages.Admin.AssetRegistry;
using BAAZ.CMMS.App.Pages.Admin.Locations;
using BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;
using BAAZ.CMMS.App.Pages.Admin.RepairDepartments;
using BAAZ.CMMS.App.Pages.Admin.Users;
using BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;
using BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;
using BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;
using BAAZ.CMMS.App.Pages.Dispatcher.PersonnelManagement;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestHistory;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;
using BAAZ.CMMS.App.Pages.Dispatcher.WorkReports;
using BAAZ.CMMS.App.Pages.Home.AdminHome;
using BAAZ.CMMS.App.Pages.Home.Dashboards;
using BAAZ.CMMS.App.Pages.Home.DispatcherHome;
using BAAZ.CMMS.App.Pages.Home.RequesterHome;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.App.Pages.Requester.NewRequest;
using BAAZ.CMMS.App.Pages.Requester.RequesterAssets;
using BAAZ.CMMS.App.Pages.Settings;
using BAAZ.CMMS.App.ViewModels;
using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Integrations.ToolIssuance;
using BAAZ.CMMS.Core.Integrations.ToolTracker;
using BAAZ.CMMS.Core.Integrations.Warehouse;
using BAAZ.CMMS.Core.Services.MaterialRequisition;
using BAAZ.CMMS.Core.Services.ToolRequisition;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Repositories;
using BAAZ.CMMS.Core.Repositories.Junction;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.TmsIssuance;
using BAAZ.CMMS.Core.Services.Integrations;
using BAAZ.CMMS.Core.Services.Catalog;
using Microsoft.Windows.AppNotifications;
using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.Services;

using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace BAAZ.CMMS.App;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        UnhandledException += HandleExceptions;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppThemeHelper.ApplySavedTheme();
        Services = ConfigureBootstrapServices();
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = LaunchAsync(dispatcherQueue);
    }

    private static ServiceProvider ConfigureBootstrapServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IGotrueSessionPersistence<Session>, WindowsCredentialSessionPersistence>();
        services.AddSingleton<ISupabaseClientProvider, SupabaseClientProvider>();
        services.AddSingleton<ISupabaseGateway, SupabaseGateway>();
        services.AddSingleton<IRepairDepartmentRepository, RepairDepartmentRepository>();
        services.AddSingleton<ITechnicianRepository, TechnicianRepository>();
        services.AddSingleton<ILocationRepository, LocationRepository>();
        services.AddSingleton<IAssetRepository, AssetRepository>();
        services.AddSingleton<IRequestRepository, RequestRepository>();
        services.AddSingleton<IWorkReportRepository, WorkReportRepository>();
        services.AddSingleton<IProfileRepository, ProfileRepository>();
        services.AddSingleton<IProfileLocationScopeRepository, ProfileLocationScopeRepository>();
        services.AddSingleton<IEquipmentCategoryRepository, EquipmentCategoryRepository>();
        services.AddSingleton<ICategoryMaintenanceNormRepository, CategoryMaintenanceNormRepository>();
        services.AddSingleton<IMaintenanceNormRepository, MaintenanceNormRepository>();
        services.AddSingleton<IAssetMaintenanceStatusRepository, AssetMaintenanceStatusRepository>();
        services.AddSingleton<IMaintenanceScheduleRepository, MaintenanceScheduleRepository>();
        services.AddSingleton(typeof(IJunctionLinkRepository<>), typeof(JunctionLinkRepository<>));
        services.AddSingleton<AdminUsersFunctionClient>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IRequestIntegrationHooks, RequestIntegrationHooks>();
        services.AddSingleton<IRequestService, RequestService>();
        RegisterCatalogServices(services);
        services.AddSingleton<ILocationTreeCache, LocationTreeCache>();
        services.AddSingleton<IRequesterAssetCatalog, RequesterAssetCatalog>();
        services.AddSingleton<LocationScopeTreeProjectionCache>();
        services.AddSingleton<IMaintenanceService, MaintenanceService>();
        services.AddSingleton<IProfileAdminService, ProfileAdminService>();
        RegisterTmsIntegration(services);
        services.AddSingleton<IToolRequisitionDocxGenerator, ToolRequisitionDocxGenerator>();
        services.AddSingleton<IToolRequisitionDocxIntegration, DocxFileToolRequisitionIntegration>();
        services.AddSingleton<ITmsToolRequisitionLinkRepository, TmsToolRequisitionLinkRepository>();
        services.AddSingleton<ITmsToolRequisitionService, TmsToolRequisitionService>();
        services.AddSingleton<IToolRequisitionService, ToolRequisitionService>();
        services.AddSingleton<IMaterialRequisitionDocxGenerator, MaterialRequisitionDocxGenerator>();
        services.AddSingleton<IWarehouseIntegration, DocxFileWarehouseIntegration>();
        services.AddSingleton<IMaterialRequisitionService, MaterialRequisitionService>();
        services.AddSingleton<IDowntimeTrackerIntegration, NullDowntimeTrackerIntegration>();
        services.AddSingleton<IRealtimeNotificationService, RealtimeNotificationService>();
        services.AddSingleton<AppBootstrapper>();

        services.AddTransient<Windows.LoginViewModel>();
        services.AddTransient<Windows.ConnectionErrorViewModel>();
        services.AddTransient<Windows.LoginWindow>();
        services.AddTransient<Windows.ConnectionErrorWindow>();
        services.AddTransient<Windows.StartupLoadingWindow>();

        return services.BuildServiceProvider();
    }

    private static ServiceProvider ConfigureShellServices(
        Frame navigationFrame,
        IWindowProvider windowProvider,
        IServiceProvider bootstrapServices)
    {
        var services = new ServiceCollection();

        services.AddSingleton(bootstrapServices.GetRequiredService<IGotrueSessionPersistence<Session>>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ISupabaseClientProvider>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ISupabaseGateway>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IRepairDepartmentRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ITechnicianRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ILocationRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IAssetRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IRequestRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IProfileRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IProfileLocationScopeRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<AdminUsersFunctionClient>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IConnectionService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IAuthService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IRequestService>());
        RegisterCatalogServices(services, bootstrapServices);
        services.AddSingleton(bootstrapServices.GetRequiredService<ILocationTreeCache>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IRequesterAssetCatalog>());
        services.AddSingleton(bootstrapServices.GetRequiredService<LocationScopeTreeProjectionCache>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IMaintenanceService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IProfileAdminService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IToolTrackerIntegration>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IToolRequisitionService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ITmsIssuanceClient>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ITmsToolRequisitionLinkRepository>());
        services.AddSingleton(bootstrapServices.GetRequiredService<ITmsToolRequisitionService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IWarehouseIntegration>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IDowntimeTrackerIntegration>());
        services.AddSingleton(bootstrapServices.GetRequiredService<IRealtimeNotificationService>());
        services.AddSingleton(bootstrapServices.GetRequiredService<AppBootstrapper>());

        services.AddSingleton<IWindowsToastService, WindowsToastService>();
        services.AddSingleton<INavBadgeService, NavBadgeService>();
        services.AddSingleton<IShellNotificationPresenter, ShellNotificationPresenter>();

        services.AddSingleton(bootstrapServices.GetRequiredService<IMaterialRequisitionService>());

        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IDocumentSaveLocationService, DocumentSaveLocationService>();
        services.AddSingleton<IWindowsShellFileService, WindowsShellFileService>();

        services.AddSingleton(navigationFrame);
        services.AddSingleton(windowProvider);
        services.AddSingleton(PageMap.Pages);
        services.AddSingleton<INavigationService, NavigationService>();

        // Home pages
        services.AddTransient<RequesterHomeDashboardViewModel>();
        services.AddTransient<DispatcherHomeDashboardViewModel>();
        services.AddTransient<AdminHomeDashboardViewModel>();
        services.AddTransient<AdminHomeViewModel>();
        services.AddTransient<DispatcherHomeViewModel>();
        services.AddTransient<RequesterHomeViewModel>();
        // Requests
        services.AddTransient<NewRequestViewModel>();
        services.AddTransient<MyRequestsViewModel>();
        services.AddTransient<MyRequestsTableViewModel>();
        services.AddTransient<RequesterAssetsViewModel>();
        services.AddTransient<IncomingRequestsViewModel>();
        services.AddTransient<RequestDetailViewModel>();
        services.AddTransient<RequestHistoryViewModel>();
        services.AddTransient<RequestHistoryTableViewModel>();
        // Maintenance
        services.AddTransient<MaintenanceScheduleViewModel>();
        services.AddTransient<WorkReportsViewModel>();
        services.AddTransient<MaintenanceNormsViewModel>();
        // Supply
        services.AddTransient<MaterialRequisitionViewModel>();
        services.AddTransient<ToolRequisitionViewModel>();
        services.AddTransient<ToolRequisitionHistoryViewModel>();
        services.AddTransient<ToolRequisitionHistoryTableViewModel>();
        // Assets & Personnel
        services.AddTransient<AssetRegistryViewModel>();
        services.AddTransient<PersonnelManagementViewModel>();
        // Admin
        services.AddTransient<LocationsViewModel>();
        services.AddTransient<RepairDepartmentsViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<AllRequestsViewModel>();
        // Shell
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<Windows.LoginViewModel>();
        services.AddTransient<Windows.ConnectionErrorViewModel>();
        services.AddTransient<Windows.LoginWindow>();
        services.AddTransient<Windows.ConnectionErrorWindow>();
        services.AddTransient<Windows.StartupLoadingWindow>();

        return services.BuildServiceProvider();
    }

    private static void RegisterCatalogServices(
        IServiceCollection services,
        IServiceProvider? existing = null)
    {
        if (existing is not null)
        {
            services.AddSingleton(existing.GetRequiredService<ICatalogLocationEnricher>());
            services.AddSingleton(existing.GetRequiredService<AssetCatalogService>());
            services.AddSingleton(existing.GetRequiredService<IAssetCatalogService>());
            services.AddSingleton(existing.GetRequiredService<LocationCatalogService>());
            services.AddSingleton(existing.GetRequiredService<ILocationCatalogService>());
            services.AddSingleton(existing.GetRequiredService<TechnicianCatalogService>());
            services.AddSingleton(existing.GetRequiredService<ITechnicianCatalogService>());
            services.AddSingleton(existing.GetRequiredService<RepairDepartmentCatalogService>());
            services.AddSingleton(existing.GetRequiredService<IRepairDepartmentCatalogService>());
            services.AddSingleton(existing.GetRequiredService<ICatalogService>());
            return;
        }

        services.AddSingleton<ICatalogLocationEnricher, CatalogLocationEnricher>();
        services.AddSingleton<AssetCatalogService>();
        services.AddSingleton<IAssetCatalogService>(sp => sp.GetRequiredService<AssetCatalogService>());
        services.AddSingleton<LocationCatalogService>();
        services.AddSingleton<ILocationCatalogService>(sp => sp.GetRequiredService<LocationCatalogService>());
        services.AddSingleton<TechnicianCatalogService>();
        services.AddSingleton<ITechnicianCatalogService>(sp => sp.GetRequiredService<TechnicianCatalogService>());
        services.AddSingleton<RepairDepartmentCatalogService>();
        services.AddSingleton<IRepairDepartmentCatalogService>(sp => sp.GetRequiredService<RepairDepartmentCatalogService>());
        services.AddSingleton<ICatalogService, CatalogService>();
    }

    private static void RegisterTmsIntegration(IServiceCollection services)
    {
        ApplyTmsIntegrationSettingsFromAppConfig();
        services.AddSingleton<TmsIssuanceClientProvider>();
        services.AddSingleton<ITmsIssuanceClient>(sp =>
        {
            var provider = sp.GetRequiredService<TmsIssuanceClientProvider>();
            TmsIntegrationSettings.RegisterIssuanceClientProvider(provider);
            return provider;
        });
        services.AddSingleton<IToolTrackerIntegration>(_ => TmsIntegrationSettings.CreateToolTrackerIntegration());
    }

    private static void ApplyTmsIntegrationSettingsFromAppConfig()
    {
        var settings = SettingsHelper.Current;
        TmsIntegrationSettingsSync.Apply(
            settings.TmsIntegrationMode,
            settings.TmsBaseUrl,
            settings.TmsIntegrationSecret,
            settings.SupabaseAnonKey);
    }

    private async Task LaunchAsync(DispatcherQueue dispatcherQueue)
    {
        try
        {
            var bootstrapServices = Services;
            var bootstrapper = bootstrapServices.GetRequiredService<AppBootstrapper>();
            Debug.WriteLine($"[App] LaunchAsync -> RunAsync");
            var bootstrapResult = await bootstrapper.RunAsync(dispatcherQueue);
            Debug.WriteLine(
                $"[App] RunAsync done, openMain={bootstrapResult.CanOpenMainWindow}, " +
                $"hideAfter={bootstrapResult.WindowToHideAfterMain?.GetType().Name ?? "none"}");
            if (!bootstrapResult.CanOpenMainWindow)
            {
                bootstrapper.CloseLoadingWindow();
                dispatcherQueue.TryEnqueue(Current.Exit);
                return;
            }

            await UiDispatchHelper.RunAsync(dispatcherQueue, () => OpenMainWindow(bootstrapServices));

            if (bootstrapResult.WindowToHideAfterMain is Window windowToHide)
            {
                await UiDispatchHelper.RunAsync(dispatcherQueue, () =>
                {
                    Debug.WriteLine($"[App] Hide {windowToHide.GetType().Name}");
                    windowToHide.AppWindow.Hide();
                });
            }

            bootstrapper.CloseLoadingWindow();
        }
        catch (Exception ex)
        {
            ReportLaunchFailure(ex);
        }
    }

    private static void OpenMainWindow(IServiceProvider bootstrapServices)
    {
        Debug.WriteLine($"[App] OpenMainWindow, thread={Environment.CurrentManagedThreadId}");
        MainWindow = new MainWindow();
        Services = ConfigureShellServices(
            MainWindow.NavigationFrame,
            new MainWindowProvider(MainWindow),
            bootstrapServices);

        MainWindow.Initialize(Services);
        WindowHelper.TrackWindow(MainWindow);
        AppThemeHelper.Apply(SettingsHelper.Current.SelectedAppTheme);
        MainWindow.Closed += (_, _) => _ = ShutdownAfterMainWindowClosedAsync();
        MainWindow.Activate();
        Debug.WriteLine($"[App] OpenMainWindow done, id={MainWindow.GetHashCode():X8}");
    }

    private static async Task ShutdownAfterMainWindowClosedAsync()
    {
        Debug.WriteLine("[App] MainWindow closed, shutting down");

        if (Services.GetService<IRealtimeNotificationService>() is { } realtime)
        {
            await realtime.StopAsync();
        }

        if (Services.GetService<IShellNotificationPresenter>() is { } presenter)
        {
            presenter.Stop();
        }

        try
        {
            AppNotificationManager.Default.Unregister();
        }
        catch
        {
            // Игнорируем при аварийном завершении.
        }

        MainWindow = null;
        Current.Exit();
    }

    private static void ReportLaunchFailure(Exception ex)
    {
        Debug.WriteLine($"Launch failed: {ex}");

        var title = ResourceStrings.Get("App_Exception_Title");
        var content = $"{ex.GetType().Name}: {ex.Message}";

        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window.Content is FrameworkElement root && root.XamlRoot is not null)
            {
                _ = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = "OK",
                    XamlRoot = root.XamlRoot,
                }.ShowAsync();
                return;
            }
        }

        Current.Exit();
    }

    private async void HandleExceptions(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        var title = ResourceStrings.Get("App_Exception_Title");
        var typeLine = ResourceStrings.Format("App_Exception_Type", e.Exception.GetType());
        var messageLine = ResourceStrings.Format("App_Exception_Message", e.Message, e.Exception.HResult);
        var content = $"{typeLine}\n{messageLine}";

        if (MainWindow?.Content is FrameworkElement root)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = root.XamlRoot,
            };
            await dialog.ShowAsync();
            return;
        }

        Debug.WriteLine($"{title}: {content}");
    }
}
