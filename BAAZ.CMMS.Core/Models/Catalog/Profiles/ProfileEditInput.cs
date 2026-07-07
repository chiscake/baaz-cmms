namespace BAAZ.CMMS.Core.Models;

public sealed class ProfileEditInput
{
    public required string FullName { get; init; }

    /// <summary>requester или dispatcher (не admin).</summary>
    public required string Role { get; init; }

    public string? Phone { get; init; }

    public Guid? LocationId { get; init; }

    /// <summary>
    /// <c>null</c> — не менять зоны заявок; пустой список — очистить.
    /// </summary>
    public IReadOnlyList<Guid>? LocationScopeIds { get; init; }

    public Guid? RepairDepartmentId { get; init; }
}
