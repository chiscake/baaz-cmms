namespace BAAZ.CMMS.Core.Models;

public sealed class CreateRequestResult
{
    public required Guid Id { get; init; }

    public required string RequestNumber { get; init; }
}
