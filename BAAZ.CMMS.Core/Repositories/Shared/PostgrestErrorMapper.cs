using System.Net;
using System.Text.Json;

using Supabase.Postgrest.Exceptions;

using BAAZ.CMMS.Core.Data;

namespace BAAZ.CMMS.Core.Repositories;

/// <summary>Общее сопоставление ошибок PostgREST → <see cref="DataError"/> для репозиториев.</summary>
public static class PostgrestErrorMapper
{
    public static DataError Map(PostgrestException ex)
    {
        if (ex.Response?.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return DataError.Unauthorized(ex.Message);

        return DataError.Unknown(ex.Message);
    }

    /// <summary>Сопоставление ошибки RPC <c>create_request</c>.</summary>
    public static DataError MapCreateRequestRpcErrorBody(string? errorBody)
    {
        var code = ExtractRpcMessage(errorBody);
        if (string.IsNullOrWhiteSpace(code))
            return DataError.Unknown(errorBody);

        if (code.Contains("PGRST202", StringComparison.Ordinal)
            || code.Contains("Could not find the function", StringComparison.Ordinal))
        {
            return DataError.Unknown(
                "create_request: схема БД устарела — выполните supabase db reset и повторите seed-test-users");
        }

        return code switch
        {
            "ASSET_NOT_ACCESSIBLE" => DataError.Validation("NewRequest_Error_AssetOrLocation", code),
            "ASSET_OR_LOCATION_REQUIRED" => DataError.Validation("NewRequest_Error_AssetOrLocation", code),
            "TARGET_DEPARTMENT_REQUIRED" => DataError.Validation("NewRequest_Error_RepairDepartment", code),
            "INVALID_TARGET_DEPARTMENT" => DataError.Validation("NewRequest_Error_RepairDepartment", code),
            "CONTRACTOR_REQUIRED" => DataError.Validation("NewRequest_Error_ContractorRequired", code),
            "UNAUTHORIZED" => DataError.Unauthorized(code),
            _ => MapRpcErrorBody(errorBody),
        };
    }

    /// <summary>
    /// Сопоставление JSON-тела ошибки PostgREST RPC (поле <c>message</c> из RAISE EXCEPTION).
    /// </summary>
    public static DataError MapRpcErrorBody(string? errorBody)
    {
        var code = ExtractRpcMessage(errorBody);
        if (string.IsNullOrWhiteSpace(code))
            return DataError.Unknown(errorBody);

        return code switch
        {
            "PENDING_SCHEDULE_EXISTS" => DataError.Validation(
                "MaintenanceSchedule_Error_PendingExists", code),
            "ASSET_NOT_AVAILABLE" => DataError.Validation(
                "MaintenanceSchedule_Error_AssetUnavailable", code),
            "UNAUTHORIZED" => DataError.Unauthorized(code),
            "WORK_REPORT_DEPARTMENT_NOT_ROUTED" => DataError.Validation(
                "WorkReport_Error_DepartmentNotRouted", code),
            "REQUEST_NOT_IN_PREPARATION" => DataError.Validation(
                "RequestDetail_Error_NotInPreparation", code),
            "DEPARTMENT_ALREADY_REPORTED" => DataError.Validation(
                "RequestDetail_Error_DepartmentAlreadyReported", code),
            "REQUEST_NOT_ACCEPTED" => DataError.Validation(
                "RequestDetail_Error_NotAccepted", code),
            "ALL_DEPARTMENTS_NEED_ASSIGNEE" => DataError.Validation(
                "RequestDetail_Error_AllDepartmentsNeedAssignee", code),
            "REQUEST_NOT_ASSIGNABLE" => DataError.Validation(
                "RequestDetail_Error_NotAssignable", code),
            _ => DataError.Unknown(code),
        };
    }

    /// <summary>Сопоставление JSON-тела ошибки PostgREST INSERT/PATCH.</summary>
    public static DataError MapPostErrorBody(string? errorBody)
    {
        var message = ExtractRpcMessage(errorBody);
        if (string.IsNullOrWhiteSpace(message))
            return DataError.Unknown(errorBody);

        if (message.Contains("WORK_REPORT_DEPARTMENT_NOT_ROUTED", StringComparison.OrdinalIgnoreCase))
            return DataError.Validation("WorkReport_Error_DepartmentNotRouted", message);

        if (message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("work_reports_schedule_dept_unique", StringComparison.OrdinalIgnoreCase)
            || message.Contains("work_reports_request_dept_unique", StringComparison.OrdinalIgnoreCase))
            return DataError.Validation("WorkReport_Error_Duplicate", message);

        return DataError.Unknown(message);
    }

    /// <summary>Сопоставление ошибки PATCH заявки (в т.ч. UNIQUE на request_number).</summary>
    public static DataError MapRequestPatchErrorBody(string? errorBody)
    {
        var message = ExtractRpcMessage(errorBody);
        if (string.IsNullOrWhiteSpace(message))
            return DataError.Unknown(errorBody);

        if (message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("request_number", StringComparison.OrdinalIgnoreCase))
                return DataError.Validation("AllRequests_Error_DuplicateNumber", message);
        }

        return MapPostErrorBody(errorBody);
    }

    public static string? ExtractRpcMessage(string? errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (doc.RootElement.TryGetProperty("message", out var messageProp))
                return messageProp.GetString();
        }
        catch (JsonException)
        {
            // plain-text fallback
        }

        return errorBody;
    }

    public static bool IsUniqueViolation(PostgrestException ex)
    {
        var message = ex.Message;
        return message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsForeignKeyViolation(PostgrestException ex)
    {
        if (ex.Response?.StatusCode == HttpStatusCode.Conflict)
            return true;

        var message = ex.Message;
        return message.Contains("23503", StringComparison.Ordinal)
            || message.Contains("foreign key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("violates foreign key constraint", StringComparison.OrdinalIgnoreCase);
    }
}
