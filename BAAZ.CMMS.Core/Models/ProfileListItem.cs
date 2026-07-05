namespace BAAZ.CMMS.Core.Models;

public sealed class ProfileListItem
{
    public Guid Id { get; init; }

    public required string Email { get; init; }

    public required string FullName { get; init; }

    public required string Role { get; init; }

    public string? Phone { get; init; }

    public Guid? LocationId { get; init; }

    public string? LocationName { get; init; }

    /// <summary>Якоря зон доступа (только requester).</summary>
    public IReadOnlyList<Guid> LocationScopeIds { get; init; } = [];

    /// <summary>Краткие подписи якорей для грида.</summary>
    public IReadOnlyList<string> LocationScopeLabels { get; init; } = [];

    public Guid? RepairDepartmentId { get; init; }

    public string? RepairDepartmentName { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public bool IsBanned { get; init; }

    /// <summary>Учётка с role=admin — только просмотр в приложении.</summary>
    public bool IsAdminAccount { get; init; }
}
