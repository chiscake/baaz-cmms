using System;
using System.Threading.Tasks;

namespace BAAZ.CMMS.App.Services.Notifications;

public interface IShellNotificationPresenter
{
    void Start();

    void Stop();

    Task SyncInitialBadgesAsync();

    void NavigateFromToast(string? pageKey, Guid? requestId);
}
