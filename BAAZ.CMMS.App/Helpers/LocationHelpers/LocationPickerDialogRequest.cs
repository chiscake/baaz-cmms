using System;
using System.Collections.Generic;

using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

public sealed class LocationPickerDialogRequest
{
    public IReadOnlyList<LocationTreeItem> TreeItems { get; init; } = [];

    public int TreeVersion { get; init; }

    public IReadOnlyDictionary<Guid, string>? LocationPaths { get; init; }

    public IReadOnlySet<Guid>? DisabledNodeIds { get; init; }

    public Guid? InitialSelection { get; init; }

    public bool AllowClearSelection { get; init; } = true;

    public string? ClearParentLabel { get; init; }

    public string? Title { get; init; }
}

public sealed class LocationScopePickerDialogRequest
{
    public IReadOnlyList<LocationTreeItem> TreeItems { get; init; } = [];

    public IReadOnlyDictionary<Guid, string>? LocationPaths { get; init; }

    public IReadOnlySet<Guid> InitialSelection { get; init; } = new HashSet<Guid>();

    public string? Title { get; init; }
}

public sealed class LocationPickerDialogResult
{
    public Guid? LocationId { get; init; }
}
