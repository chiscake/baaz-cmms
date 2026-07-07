using System;
using System.Collections.Generic;

using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Стандартные определения колонок CrudWorkbench.</summary>
public static class CrudColumnTemplates
{
    public const string UuidHeaderResourceKey = "CrudGrid_Column_Id";

    /// <summary>
    /// Скрытая по умолчанию колонка <c>Id</c> (uuid) — всегда последняя в <c>InitColumns</c>.
    /// </summary>
    public static CrudColumnDefinition CreateHiddenUuidColumn(Action<CrudColumnDefinition>? configure = null)
    {
        var col = new CrudColumnDefinition
        {
            Key = "Id",
            Header = ResourceStrings.Get(UuidHeaderResourceKey),
            DataTypeLabel = "uuid",
            DesiredWidth = 280,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
            IsVisibleByDefault = false,
            IsPrimaryKey = true,
        };
        configure?.Invoke(col);
        return col;
    }

    public static CrudColumnDefinition CreateActiveBoolColumn(string header, Action<CrudColumnDefinition>? configure = null)
    {
        var col = new CrudColumnDefinition
        {
            Key = "Active",
            Header = header,
            DataTypeLabel = "bool",
            DesiredWidth = 70,
            IsSortable = false,
            EditKind = CrudColumnEditKind.ReadOnly,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Bool,
        };
        configure?.Invoke(col);
        return col;
    }

    /// <summary>Добавляет CreatedAt, UpdatedAt и скрытый Id (порядок сохраняется).</summary>
    public static void AppendAuditColumns(
        IList<CrudColumnDefinition> columns,
        string createdAtHeader,
        string updatedAtHeader,
        Action<CrudColumnDefinition>? configureCreatedAt = null,
        Action<CrudColumnDefinition>? configureUpdatedAt = null)
    {
        var createdAt = new CrudColumnDefinition
        {
            Key = "CreatedAt",
            Header = createdAtHeader,
            DataTypeLabel = "timestamptz",
            DesiredWidth = 160,
            EditKind = CrudColumnEditKind.ReadOnly,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = false,
        };
        configureCreatedAt?.Invoke(createdAt);
        columns.Add(createdAt);

        var updatedAt = new CrudColumnDefinition
        {
            Key = "UpdatedAt",
            Header = updatedAtHeader,
            DataTypeLabel = "timestamptz",
            DesiredWidth = 160,
            EditKind = CrudColumnEditKind.ReadOnly,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = false,
        };
        configureUpdatedAt?.Invoke(updatedAt);
        columns.Add(updatedAt);

        columns.Add(CreateHiddenUuidColumn());
    }
}
