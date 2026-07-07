using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BAAZ.CMMS.Core.Data.Attributes;

using Supabase.Postgrest.Attributes;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Проставляет флаги PK/Unique на <see cref="CrudColumnDefinition"/> из моделей и конвенций.</summary>
public static class CrudColumnSemantics
{
    public static void ApplyFromModel(IList<CrudColumnDefinition> columns, Type modelType)
    {
        var byKey = columns.ToDictionary(c => c.Key, StringComparer.Ordinal);

        foreach (var prop in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!byKey.TryGetValue(prop.Name, out var col))
                continue;

            if (prop.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
                col.IsPrimaryKey = true;

            if (prop.GetCustomAttribute<UniqueAttribute>() is not null)
                col.IsUnique = true;
        }
    }

    public static void ApplyIdConvention(IList<CrudColumnDefinition> columns)
    {
        foreach (var col in columns)
        {
            if (col.Key.Equals("Id", StringComparison.OrdinalIgnoreCase))
                col.IsPrimaryKey = true;
        }
    }

    public static void ApplyManual(IList<CrudColumnDefinition> columns, params (string Key, bool Unique)[] overrides)
    {
        var byKey = columns.ToDictionary(c => c.Key, StringComparer.Ordinal);
        foreach (var (key, unique) in overrides)
        {
            if (byKey.TryGetValue(key, out var col) && unique)
                col.IsUnique = true;
        }
    }
}
