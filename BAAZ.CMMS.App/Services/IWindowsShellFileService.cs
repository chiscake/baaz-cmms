using System.Threading;
using System.Threading.Tasks;

namespace BAAZ.CMMS.App.Services;

public interface IWindowsShellFileService
{
    Task OpenFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task RevealInExplorerAsync(string filePath, CancellationToken cancellationToken = default);
}
