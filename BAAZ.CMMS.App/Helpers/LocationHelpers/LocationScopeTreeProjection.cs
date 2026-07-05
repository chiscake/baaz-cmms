using System;
using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.App.Controls.LocationScopePicker;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>UI-проекция дерева зон заявок; строится один раз на Version снимка.</summary>
public sealed class LocationScopeTreeProjection
{
    public int Version { get; init; }

    public IReadOnlyList<LocationScopeNode> Roots { get; init; } = [];

    public IReadOnlyDictionary<Guid, LocationScopeNode> NodesById { get; init; } =
        new Dictionary<Guid, LocationScopeNode>();

    public static readonly LocationScopeTreeProjection Empty = new();

    public static LocationScopeTreeProjection Build(
        IReadOnlyList<LocationTreeItem> roots,
        int version)
    {
        var nodesById = new Dictionary<Guid, LocationScopeNode>();
        var scopeRoots = roots.Select(r => MapNode(r, nodesById)).ToList();

        return new LocationScopeTreeProjection
        {
            Version = version,
            Roots = scopeRoots,
            NodesById = nodesById,
        };
    }

    private static LocationScopeNode MapNode(
        LocationTreeItem item,
        IDictionary<Guid, LocationScopeNode> nodesById)
    {
        var node = new LocationScopeNode
        {
            Id = item.Id,
            Name = item.Name,
            IsEnabled = item.IsEnabled,
            Children = item.Children.Select(c => MapNode(c, nodesById)).ToList(),
        };

        nodesById[item.Id] = node;
        return node;
    }
}

/// <summary>Кэш проекции scope-дерева по Version снимка локаций.</summary>
public sealed class LocationScopeTreeProjectionCache
{
    private int _cachedVersion;
    private LocationScopeTreeProjection? _cached;

    public LocationScopeTreeProjection Get(LocationTreeSnapshot snapshot) =>
        Get(snapshot.ActiveRoots, snapshot.Version);

    public LocationScopeTreeProjection Get(
        IReadOnlyList<LocationTreeItem> roots,
        int version)
    {
        if (_cached is not null && _cachedVersion == version)
            return _cached;

        _cachedVersion = version;
        _cached = LocationScopeTreeProjection.Build(roots, version);
        return _cached;
    }
}
