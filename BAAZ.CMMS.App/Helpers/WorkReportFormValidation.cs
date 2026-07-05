using System;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers;

internal static class WorkReportFormValidation
{
    public static string? Validate(
        bool technicianSelected,
        string? workPerformed,
        double durationHours,
        RepairDepartmentListItem? department = null,
        bool departmentRequired = false)
    {
        if (departmentRequired && department is null)
            return ResourceStrings.Get("WorkReport_Error_DepartmentRequired");

        if (!technicianSelected)
            return ResourceStrings.Get("WorkReport_Validation_TechnicianNotAssigned");

        if (string.IsNullOrWhiteSpace(workPerformed))
            return ResourceStrings.Get("WorkReport_Validation_WorkPerformedRequired");

        if (double.IsNaN(durationHours))
            return ResourceStrings.Get("WorkReport_Validation_DurationRequired");

        if (durationHours <= 0)
            return ResourceStrings.Get("WorkReport_Validation_DurationPositive");

        return null;
    }

    public static string ResolveError(string? messageKey, string? fallback, string genericFallbackKey)
    {
        if (!string.IsNullOrWhiteSpace(messageKey))
        {
            var localized = ResourceStrings.Get(messageKey);
            if (!string.Equals(localized, messageKey, StringComparison.Ordinal))
                return localized;
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            if (fallback.Contains("WORK_REPORT_DEPARTMENT_NOT_ROUTED", StringComparison.OrdinalIgnoreCase))
                return ResourceStrings.Get("WorkReport_Error_DepartmentNotRouted");
            if (fallback.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || fallback.Contains("23505", StringComparison.Ordinal))
                return ResourceStrings.Get("WorkReport_Error_Duplicate");
        }

        return ResourceStrings.Get(genericFallbackKey);
    }
}
