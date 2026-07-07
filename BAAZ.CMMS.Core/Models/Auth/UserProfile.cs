namespace BAAZ.CMMS.Core.Models;

public sealed class UserProfile
{
    public required Guid Id { get; init; }

    public required UserRole Role { get; init; }

    public string? FullName { get; init; }

    public Guid? LocationId { get; init; }

    public string? LocationName { get; init; }

    public Guid? RepairDepartmentId { get; init; }

    public string? RepairDepartmentName { get; init; }

    /// <summary>Якоря зон доступа заявителя (profile_location_scopes).</summary>
    public IReadOnlyList<Guid> LocationScopeIds { get; init; } = [];
}
