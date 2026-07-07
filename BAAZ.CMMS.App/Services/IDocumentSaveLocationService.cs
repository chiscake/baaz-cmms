using System.Threading;
using System.Threading.Tasks;

namespace BAAZ.CMMS.App.Services;

public interface IDocumentSaveLocationService
{
    Task<string?> PickDocxSavePathAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);

    Task<string?> PickXlsxSavePathAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);

    string? LastSaveDirectory { get; }
}
