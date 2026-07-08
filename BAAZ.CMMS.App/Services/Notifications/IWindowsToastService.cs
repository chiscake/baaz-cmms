using System;

namespace BAAZ.CMMS.App.Services.Notifications;

public interface IWindowsToastService
{
    void ShowRequestNew(Guid requestId, string requestNumber);

    void ShowRequestStatusChanged(Guid requestId, string requestNumber, string statusLabel);

    void ShowScheduleUpdated();

    void ShowToolRequisitionReady(Guid linkId, string requisitionNumber, string statusLabel);
}
