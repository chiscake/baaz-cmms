using System.Collections.Generic;

namespace BAAZ.CMMS.Core.Models;

/// <summary>Узел дерева локаций для UI (ParentPicker).</summary>
public sealed class LocationTreeItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public bool IsEnabled { get; init; } = true;

    public IList<LocationTreeItem> Children { get; init; } = [];
}
