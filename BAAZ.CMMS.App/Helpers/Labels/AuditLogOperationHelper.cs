using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Helpers;

internal static class AuditLogOperationHelper
{
    public static string GetLabel(string operation)
        => operation switch
        {
            "INSERT" => ResourceStrings.Get("AuditLog_Operation_Create"),
            "UPDATE" => ResourceStrings.Get("AuditLog_Operation_Update"),
            "DELETE" => ResourceStrings.Get("AuditLog_Operation_Delete"),
            _ => operation,
        };
}
