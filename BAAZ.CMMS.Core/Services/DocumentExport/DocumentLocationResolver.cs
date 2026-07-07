using BAAZ.CMMS.Core.Helpers;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

internal static class DocumentLocationResolver
{
    public const string PathSeparator = ", ";

    public static async Task<string> ResolveForRequestAsync(
        RequestDetailItem detail,
        IAssetRepository assetRepository,
        ILocationTreeCache locationTreeCache,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();

        if (!IsPlaceholder(detail.LocationDescription))
            parts.Add(detail.LocationDescription.Trim());

        if (detail.AssetId is { } assetId)
        {
            var assetResult = await assetRepository.GetByIdAsync(assetId, cancellationToken);
            if (assetResult.IsSuccess
                && assetResult.Value?.LocationId is { } locationId)
            {
                var snapshot = await locationTreeCache.EnsureLoadedAsync(cancellationToken);
                var paths = LocationHierarchyHelper.BuildFullPaths(snapshot.AllItems, PathSeparator);
                if (paths.TryGetValue(locationId, out var assetPath))
                    parts.Add(assetPath);
            }
        }

        return parts.Count > 0 ? string.Join(PathSeparator, parts) : "—";
    }

    private static bool IsPlaceholder(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var trimmed = text.Trim();
        return trimmed is "—" or "-" or "–";
    }
}
