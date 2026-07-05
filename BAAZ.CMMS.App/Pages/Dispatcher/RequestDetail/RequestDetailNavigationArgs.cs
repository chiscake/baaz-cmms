using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;

public sealed class RequestDetailNavigationArgs
{
    public required Guid RequestId { get; init; }
}
