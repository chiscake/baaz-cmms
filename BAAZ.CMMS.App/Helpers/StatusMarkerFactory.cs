namespace BAAZ.CMMS.App.Helpers;

/// <summary>Фабрика цветных меток (кружков) для категории, приоритета и статуса заявки.</summary>
public static class StatusMarkerFactory
{
    public static StatusMarkerStyle ForRequestType(string? type) => type switch
    {
        "breakdown" => new(StatusBadgeColorToken.Orange700, RequestEnumLabels.Type(type)),
        "service" => new(StatusBadgeColorToken.Blue, RequestEnumLabels.Type(type)),
        "inspection" => new(StatusBadgeColorToken.Green, RequestEnumLabels.Type(type)),
        _ => new(StatusBadgeColorToken.BlueGrey, RequestEnumLabels.Type(type)),
    };

    public static StatusMarkerStyle ForRequestPriority(string? priority) => priority switch
    {
        "critical" => new(StatusBadgeColorToken.Red, RequestEnumLabels.Priority(priority)),
        "high" => new(StatusBadgeColorToken.Orange700, RequestEnumLabels.Priority(priority)),
        "normal" => new(StatusBadgeColorToken.Amber, RequestEnumLabels.Priority(priority)),
        "low" => new(StatusBadgeColorToken.Green, RequestEnumLabels.Priority(priority)),
        _ => new(StatusBadgeColorToken.BlueGrey, RequestEnumLabels.Priority(priority)),
    };

    public static StatusMarkerStyle ForRequestStatus(string? status)
    {
        var badge = StatusBadgeFactory.ForRequest(status);
        return new(badge.Background, RequestStatusHelper.GetLabel(status));
    }
}
