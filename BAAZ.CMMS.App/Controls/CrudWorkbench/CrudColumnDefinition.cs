using System;
using System.Collections.Generic;

using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Описание видимой колонки таблицы (метаданные для UI).</summary>
public sealed class CrudColumnDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;

    /// <summary>Краткий тип для отображения под заголовком (например «text», «bool»).</summary>
    public string? DataTypeLabel { get; set; }

    /// <summary>Первичный ключ записи (бейдж «ключ» в заголовке).</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>UNIQUE в БД (бейдж «уникальное»; не дублируется для PK).</summary>
    public bool IsUnique { get; set; }

    public bool IsVisible { get; set; } = true;

    /// <summary>Видимость после сброса к умолчанию.</summary>
    public bool IsVisibleByDefault { get; set; } = true;

    /// <summary>
    /// Служебная колонка: не отображается в таблице и недоступна в picker «Столбцы».
    /// Ограничение доступа к данным — через RLS/роль; здесь только UI.
    /// </summary>
    public bool IsHidden { get; set; }

    public bool IsSortable { get; set; } = true;
    public bool IsInlineEditable { get; set; }

    /// <summary>Колонка доступна в панели фильтров Supabase-style.</summary>
    public bool IsFilterable { get; set; }

    public CrudColumnFilterKind FilterKind { get; set; } = CrudColumnFilterKind.Text;

    public CrudColumnEditKind EditKind { get; set; } = CrudColumnEditKind.Text;
    public IReadOnlyList<CrudEnumOption>? EnumOptions { get; set; }

    /// <summary>Корни дерева для <see cref="CrudColumnEditKind.LocationTree"/>.</summary>
    public IReadOnlyList<LocationTreeItem>? LocationTreeRoots { get; set; }

    /// <summary>Версия снимка каталога для <see cref="CrudColumnEditKind.LocationTree"/>.</summary>
    public int LocationTreeVersion { get; set; }

    /// <summary>Полные пути локаций для поиска в inline/flyout picker.</summary>
    public IReadOnlyDictionary<Guid, string>? LocationPaths { get; set; }

    /// <summary>Недоступные узлы дерева локаций (inline-flyout).</summary>
    public IReadOnlySet<Guid>? DisabledLocationNodeIds { get; set; }

    /// <summary>Разрешить сброс выбора локации в inline-flyout.</summary>
    public bool AllowClearLocationSelection { get; set; }

    /// <summary>Кэшированная проекция scope-дерева для <see cref="CrudColumnEditKind.LocationScopeTree"/>.</summary>
    public LocationScopeTreeProjection? ScopeTreeProjection { get; set; }

    /// <summary>Желаемая ширина в пикселях. NaN = star (растяжимая).</summary>
    public double DesiredWidth { get; set; } = double.NaN;

    /// <summary>Вычисляемое поле (не в БД); inline-edit запрещён.</summary>
    public bool IsComputed { get; set; }

    /// <summary>Максимальная длина для TextBox в inline-flyout (<see cref="CrudColumnEditKind.Text"/>).</summary>
    public int? MaxLength { get; set; }

    /// <summary>Эффективная начальная ширина в пикселях (NaN → 150px).</summary>
    public double GetEffectiveWidth() =>
        !double.IsNaN(DesiredWidth) ? DesiredWidth : 150;
}
