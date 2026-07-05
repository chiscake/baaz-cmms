namespace BAAZ.CMMS.App.Helpers;

/// <summary>Единая палитра бейджей статусов заявок, графика ТО и оборудования (Material Design, см. <see cref="SemanticColorCatalog"/>).</summary>
public static class StatusBadgeFactory
{
    public const string DefaultBackgroundKey = nameof(StatusBadgeColorToken.BlueGrey);
    public const string DefaultForegroundKey = nameof(StatusBadgeColorToken.OnBadgeText);

    /// <summary>Нейтральный счётчик (×N, колонки канбана) — Material BlueGrey, контрастен на светлой сетке.</summary>
    public const string NeutralBadgeBackgroundKey = DefaultBackgroundKey;
    public const string NeutralBadgeForegroundKey = DefaultForegroundKey;

    public static StatusBadgeStyle ForChartMarkerCount() =>
        new(StatusBadgeColorToken.BlueGrey);

    public static StatusBadgeStyle ForRequest(string? status) => status switch
    {
        "new" => new(StatusBadgeColorToken.Blue),
        "accepted" => new(StatusBadgeColorToken.BlueGrey),
        "in_progress" => new(StatusBadgeColorToken.Amber),
        "completed" => new(StatusBadgeColorToken.Green),
        "closed" => new(StatusBadgeColorToken.Teal),
        "rejected" => new(StatusBadgeColorToken.Red),
        "cancelled" => new(StatusBadgeColorToken.Brown),
        _ => new(StatusBadgeColorToken.BlueGrey),
    };

    public static StatusBadgeStyle ForRequestType(string? type) => type switch
    {
        "breakdown" => new(StatusBadgeColorToken.Orange700),
        "service" => new(StatusBadgeColorToken.Blue),
        "inspection" => new(StatusBadgeColorToken.Green),
        _ => new(StatusBadgeColorToken.BlueGrey),
    };

    public static StatusBadgeStyle ForSchedule(string? status) => status switch
    {
        "scheduled" => new(StatusBadgeColorToken.Blue),
        "overdue" => new(StatusBadgeColorToken.Red),
        "in_progress" => new(StatusBadgeColorToken.Amber),
        "completed" => new(StatusBadgeColorToken.Green),
        "cancelled" => new(StatusBadgeColorToken.Brown),
        _ => new(StatusBadgeColorToken.BlueGrey),
    };

    public static StatusBadgeStyle ForAsset(string? status) => status switch
    {
        "active" => new(StatusBadgeColorToken.Green),
        "maintenance" => new(StatusBadgeColorToken.Amber),
        "decommissioned" => new(StatusBadgeColorToken.Brown),
        _ => new(StatusBadgeColorToken.BlueGrey),
    };
}
