namespace BAAZ.CMMS.Core.Configuration;

public static class SupabaseDefaults
{
#if DEBUG
    public const string Url = "http://127.0.0.1:54321";

    public const string PublishableKey = "sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH";
#else
    public const string Url = "https://nuygawdgrzoiehefysfv.supabase.co";

    public const string PublishableKey = "sb_publishable_398T8JeReGuEb9euLa8Xfw_RcGxT6tq";
#endif
}
