using System;
using System.Diagnostics;

using Helpers.Settings;

using Microsoft.Windows.Globalization;

namespace BAAZ.CMMS.App.Localization;

public static class LanguageHelper
{
    public const string SystemLanguageTag = "system";

    public const string RussianTag = "ru-RU";

    public const string EnglishTag = "en-US";

    public static void ApplySavedLanguage()
    {
        var tag = NormalizeLanguageTag(SettingsHelper.Current.AppLanguage);

        // MRT Core rejects empty string and non-override tags (see ApplicationLanguages.cpp).
        // ru-RU is DefaultLanguage — omit override. "system" uses the default resolution chain.
        if (!tag.Equals(EnglishTag, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            ApplicationLanguages.PrimaryLanguageOverride = EnglishTag;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplySavedLanguage failed for '{tag}': {ex}");
        }
    }

    public static int LanguageToIndex(string tag) => NormalizeLanguageTag(tag) switch
    {
        RussianTag => 1,
        EnglishTag => 2,
        _ => 0,
    };

    public static string IndexToLanguageTag(int index) => index switch
    {
        1 => RussianTag,
        2 => EnglishTag,
        _ => SystemLanguageTag,
    };

    private static string NormalizeLanguageTag(string? tag) => tag switch
    {
        null or "" => SystemLanguageTag,
        var t when t.Equals(RussianTag, StringComparison.OrdinalIgnoreCase) => RussianTag,
        var t when t.Equals(EnglishTag, StringComparison.OrdinalIgnoreCase) => EnglishTag,
        var t when t.Equals(SystemLanguageTag, StringComparison.OrdinalIgnoreCase) => SystemLanguageTag,
        _ => SystemLanguageTag,
    };
}
