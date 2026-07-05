namespace BAAZ.CMMS.Core.Models;

/// <summary>Поля заявки, доступные админу для редактирования через REST PATCH.</summary>
public sealed class RequestEditInput
{
    public required string RequestNumber { get; init; }

    public required string Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public required string Type { get; init; }

    public required string Priority { get; init; }
}
