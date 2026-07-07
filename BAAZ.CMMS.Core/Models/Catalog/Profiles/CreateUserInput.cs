namespace BAAZ.CMMS.Core.Models;

public sealed class CreateUserInput
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public required string FullName { get; init; }

    /// <summary>requester или dispatcher (не admin).</summary>
    public required string Role { get; init; }

    public required Guid LocationId { get; init; }

    public IReadOnlyList<Guid> LocationScopeIds { get; init; } = [];

    public string? Phone { get; init; }

    public Guid? RepairDepartmentId { get; init; }
}
