using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BAAZ.CMMS.App.Services;

public sealed class WindowsShellFileService : IWindowsShellFileService
{
    public Task OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Process.Start(new ProcessStartInfo(filePath)
        {
            UseShellExecute = true,
        });

        return Task.CompletedTask;
    }

    public Task RevealInExplorerAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
        {
            UseShellExecute = true,
        });

        return Task.CompletedTask;
    }
}
