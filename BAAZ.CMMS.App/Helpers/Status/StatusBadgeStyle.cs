namespace BAAZ.CMMS.App.Helpers;

/// <summary>Material palette tokens for pill status badge background and text.</summary>
public readonly record struct StatusBadgeStyle(
    StatusBadgeColorToken Background,
    StatusBadgeColorToken Foreground = StatusBadgeColorToken.OnBadgeText)
{
    public string BackgroundKey => Background.ToString();

    public string ForegroundKey => Foreground.ToString();
}
