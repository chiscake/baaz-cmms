namespace Helpers.Settings;

public sealed partial class SettingsHelper : SettingsHelperBase<SettingsHelper>
{
    public static void Initialize()
    {
        Initialize(provider => new SettingsHelper(provider));
    }

    private SettingsHelper(ISettingsProvider provider)
        : base(provider)
    {
    }
}
