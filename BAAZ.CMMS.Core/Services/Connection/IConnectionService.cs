namespace BAAZ.CMMS.Core.Services;

public interface IConnectionService
{
    event EventHandler<bool>? ConnectionStateChanged;

    bool IsConnected { get; }

    Task<bool> CheckAsync(CancellationToken cancellationToken = default);
}
