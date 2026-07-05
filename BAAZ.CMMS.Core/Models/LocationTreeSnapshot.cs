namespace BAAZ.CMMS.Core.Models;

/// <summary>Снимок справочника локаций для UI-деревьев (session-scoped кэш).</summary>
public sealed class LocationTreeSnapshot
{
    public required int Version { get; init; }

    public required IReadOnlyList<LocationListItem> AllItems { get; init; }

    public required IReadOnlyList<LocationTreeItem> ActiveRoots { get; init; }

    public required IReadOnlyDictionary<Guid, string> FullPaths { get; init; }

    public required IReadOnlyDictionary<Guid, LocationListItem> ById { get; init; }
}
