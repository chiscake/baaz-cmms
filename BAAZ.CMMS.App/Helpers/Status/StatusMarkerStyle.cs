namespace BAAZ.CMMS.App.Helpers;

/// <summary>Цвет и подсказка круговой метки.</summary>
public readonly record struct StatusMarkerStyle(StatusBadgeColorToken Color, string Tooltip)
{
    public string ColorKey => Color.ToString();
}
