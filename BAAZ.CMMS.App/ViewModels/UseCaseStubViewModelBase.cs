using System.Collections.Generic;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.ViewModels;

/// <summary>
/// Базовый ViewModel для stub-страниц UC.
/// <para>
/// <b>PlannedDescription</b> — текст TextBlock, описывающий, что будет на этой странице (из overview).
/// </para>
/// <para>
/// <b>PendingFeatures</b> — список строк для красных TextBlock на частично реализованных страницах
/// (страницы, где есть реализация, но UC полностью не покрыт).
/// </para>
/// </summary>
public abstract class UseCaseStubViewModelBase : PageViewModelBase
{
    /// <summary>Описание планируемой функциональности страницы.</summary>
    public abstract string PlannedDescription { get; }

    /// <summary>Список незавершённых функций. Пустой на чистых stub-страницах.</summary>
    public virtual IReadOnlyList<string> PendingFeatures => [];

    /// <summary>True если есть незавершённые функции (используется для Visibility в XAML).</summary>
    public bool HasPendingFeatures => PendingFeatures.Count > 0;
}
