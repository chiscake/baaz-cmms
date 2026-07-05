using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;

public static class ScheduleSwimlaneBuilder
{
    public sealed class BuildInput
    {
        public required IReadOnlyList<LocationTreeItem> LocationRoots { get; init; }

        public required IReadOnlyDictionary<Guid, LocationListItem> LocationsById { get; init; }

        public required IReadOnlyList<MaintenanceScheduleItem> ScheduleItems { get; init; }

        public required ScheduleTimelineScale Scale { get; init; }

        public required IReadOnlySet<Guid> CollapsedLocationIds { get; init; }

        public required DateOnly Today { get; init; }
    }

    public static IReadOnlyList<ChartLaneRowVm> Build(BuildInput input)
    {
        var assetsByLocation = input.ScheduleItems
            .GroupBy(i => i.LocationId ?? Guid.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(i => i.AssetId)
                    .Select(ag => ag.First())
                    .OrderBy(a => a.AssetNumber, StringComparer.CurrentCultureIgnoreCase)
                    .ToList());

        var assetIdsInSchedule = input.ScheduleItems
            .Select(i => i.AssetId)
            .ToHashSet();

        var markersByAssetDate = BuildMarkersByAssetDate(input);

        var rows = new List<ChartLaneRowVm>();
        foreach (var root in input.LocationRoots)
            AppendLocationNode(root, 0, null, input, assetsByLocation, assetIdsInSchedule, markersByAssetDate, rows);

        return rows;
    }

    public static IReadOnlyList<ChartHeatSegmentVm> BuildHeatMap(
        IReadOnlyList<MaintenanceScheduleItem> items,
        ScheduleTimelineScale scale,
        DateOnly today)
    {
        var segments = new List<ChartHeatSegmentVm>();
        for (var d = scale.RangeStart; d <= scale.RangeEnd; d = d.AddDays(1))
        {
            var dayItems = items.Where(i => i.PlannedDate == d).ToList();
            string? brushKey = null;
            if (dayItems.Count > 0)
            {
                var dominant = ScheduleMarkerPriority.ResolveDominantStatus(
                    dayItems.Select(i => (i.Status, i.PlannedDate)),
                    today);
                brushKey = StatusBadgeFactory.ForSchedule(dominant).BackgroundKey;
            }

            segments.Add(new ChartHeatSegmentVm
            {
                Date = d,
                StatusBrushKey = brushKey,
                Left = (d.DayNumber - scale.RangeStart.DayNumber) * scale.DayWidth,
                Width = scale.DayWidth,
            });
        }

        return segments;
    }

    private static void AppendLocationNode(
        LocationTreeItem node,
        int depth,
        Guid? parentLocationId,
        BuildInput input,
        IReadOnlyDictionary<Guid, List<MaintenanceScheduleItem>> assetsByLocation,
        HashSet<Guid> assetIdsInSchedule,
        IReadOnlyDictionary<(Guid AssetId, DateOnly Date), ChartMarkerVm> markersByAssetDate,
        List<ChartLaneRowVm> rows)
    {
        var hasChildLocations = node.Children.Count > 0;
        var directAssets = assetsByLocation.TryGetValue(node.Id, out var assets)
            ? assets.Where(a => assetIdsInSchedule.Contains(a.AssetId)).ToList()
            : [];

        var hasRelevantDescendants = HasRelevantContent(node, assetsByLocation, assetIdsInSchedule);
        if (!hasRelevantDescendants)
            return;

        var isCollapsed = input.CollapsedLocationIds.Contains(node.Id);
        rows.Add(new ChartLaneRowVm
        {
            Kind = ChartLaneRowKind.Location,
            Id = node.Id,
            ParentLocationId = parentLocationId,
            Label = node.Name,
            IndentLevel = depth,
            HasChildren = hasChildLocations || directAssets.Count > 0,
            IsCollapsed = isCollapsed,
        });

        if (isCollapsed)
            return;

        foreach (var asset in directAssets)
        {
            rows.Add(BuildAssetRow(asset, depth + 1, node.Id, input.Scale, markersByAssetDate));
        }

        foreach (var child in node.Children)
            AppendLocationNode(child, depth + 1, node.Id, input, assetsByLocation, assetIdsInSchedule, markersByAssetDate, rows);
    }

    private static bool HasRelevantContent(
        LocationTreeItem node,
        IReadOnlyDictionary<Guid, List<MaintenanceScheduleItem>> assetsByLocation,
        HashSet<Guid> assetIdsInSchedule)
    {
        if (assetsByLocation.TryGetValue(node.Id, out var direct)
            && direct.Any(a => assetIdsInSchedule.Contains(a.AssetId)))
            return true;

        return node.Children.Any(c => HasRelevantContent(c, assetsByLocation, assetIdsInSchedule));
    }

    private static ChartLaneRowVm BuildAssetRow(
        MaintenanceScheduleItem assetSample,
        int depth,
        Guid locationId,
        ScheduleTimelineScale scale,
        IReadOnlyDictionary<(Guid AssetId, DateOnly Date), ChartMarkerVm> markersByAssetDate)
    {
        var markers = new List<ChartMarkerVm>();
        for (var d = scale.RangeStart; d <= scale.RangeEnd; d = d.AddDays(1))
        {
            if (markersByAssetDate.TryGetValue((assetSample.AssetId, d), out var marker))
                markers.Add(marker);
        }

        return new ChartLaneRowVm
        {
            Kind = ChartLaneRowKind.Asset,
            Id = assetSample.AssetId,
            AssetId = assetSample.AssetId,
            ParentLocationId = locationId,
            Label = $"{assetSample.AssetName} ({assetSample.AssetNumber})",
            IndentLevel = depth,
            Markers = markers,
        };
    }

    private static Dictionary<(Guid AssetId, DateOnly Date), ChartMarkerVm> BuildMarkersByAssetDate(BuildInput input)
    {
        var grouped = input.ScheduleItems
            .Where(i => input.Scale.Contains(i.PlannedDate))
            .GroupBy(i => (i.AssetId, i.PlannedDate));

        var result = new Dictionary<(Guid, DateOnly), ChartMarkerVm>();
        foreach (var group in grouped)
        {
            var events = group.ToList();
            var dominant = ScheduleMarkerPriority.ResolveDominantStatus(
                events.Select(e => (e.Status, e.PlannedDate)),
                input.Today);
            var brushKey = StatusBadgeFactory.ForSchedule(dominant).BackgroundKey;

            var sameDay = events.Select(e => new ChartMarkerVm
            {
                ScheduleId = e.Id,
                AssetId = e.AssetId,
                PlannedDate = e.PlannedDate,
                Status = e.Status,
                StatusBrushKey = StatusBadgeFactory.ForSchedule(e.Status).BackgroundKey,
                MaintenanceTypeLabel = MaintenanceTypeLabels.Get(e.MaintenanceType),
                AssetName = e.AssetName,
                StatusLabel = MaintenanceTypeLabels.ScheduleStatus(e.Status),
                EventCount = 1,
            }).ToList();

            var primary = sameDay.OrderBy(m => ScheduleMarkerPriority.StatusPriority(m.Status)).First();
            result[group.Key] = new ChartMarkerVm
            {
                ScheduleId = primary.ScheduleId,
                AssetId = primary.AssetId,
                PlannedDate = primary.PlannedDate,
                Status = dominant,
                StatusBrushKey = brushKey,
                MaintenanceTypeLabel = primary.MaintenanceTypeLabel,
                AssetName = primary.AssetName,
                StatusLabel = MaintenanceTypeLabels.ScheduleStatus(dominant),
                EventCount = sameDay.Count,
                SameDayEvents = sameDay,
            };
        }

        return result;
    }
}
