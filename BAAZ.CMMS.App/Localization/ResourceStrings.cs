using Microsoft.Windows.ApplicationModel.Resources;

namespace BAAZ.CMMS.App.Localization;

public static class ResourceStrings
{
    private static readonly ResourceLoader Loader = new();

    public static string Get(string key) => Loader.GetString(key);

    public static string Format(string key, params object[] args) =>
        string.Format(Get(key), args);
}
