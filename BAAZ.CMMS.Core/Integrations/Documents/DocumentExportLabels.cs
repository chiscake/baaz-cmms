using System.Linq;

namespace BAAZ.CMMS.Core.Integrations.Documents;

/// <summary>Русские подписи enum-значений для печатных форм (фиксированный текст в DOCX/XLSX).</summary>
public static class DocumentExportLabels
{
    public static string RequestStatus(string? value) => value switch
    {
        "new" => "Новая",
        "accepted" => "Принята",
        "in_progress" => "В работе",
        "completed" => "Выполнена",
        "closed" => "Закрыта",
        "rejected" => "Отклонена",
        "cancelled" => "Отменена",
        _ => value ?? string.Empty,
    };

    public static string RequestType(string? value) => value switch
    {
        "breakdown" => "Поломка",
        "service" => "Обслуживание",
        "inspection" => "Осмотр",
        _ => value ?? string.Empty,
    };

    public static string RequestPriority(string? value) => value switch
    {
        "low" => "Низкий",
        "normal" => "Обычный",
        "high" => "Высокий",
        "critical" => "Критический",
        _ => value ?? string.Empty,
    };

    public static string RepairZone(string? value) => value switch
    {
        "on_site" => "На месте",
        "workshop" => "В ремонтном цехе",
        "external" => "У внешнего подрядчика",
        _ => value ?? string.Empty,
    };

    public static string MaintenanceType(string? value) => value switch
    {
        "to1" => "ТО-1",
        "to2" => "ТО-2",
        "kr" => "КР",
        _ => value ?? string.Empty,
    };

    public static string ScheduleStatus(string? value) => value switch
    {
        "scheduled" => "Запланировано",
        "in_progress" => "В работе",
        "overdue" => "Просрочено",
        "completed" => "Выполнено",
        "cancelled" => "Отменено",
        _ => value ?? string.Empty,
    };

    public static string FormatMaintenanceTypes(IReadOnlyList<string>? types, string? singleType)
    {
        if (types is { Count: > 0 })
            return string.Join(", ", types.Select(MaintenanceType));

        return string.IsNullOrWhiteSpace(singleType)
            ? string.Empty
            : MaintenanceType(singleType);
    }
}
