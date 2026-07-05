using System;

namespace BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;

/// <summary>
/// Контракт deep-link на страницу нормативов ТО (UC-A5). Приём реализован в
/// <see cref="MaintenanceNormsViewModel.ApplyNavigationArgsAsync"/>: <c>AssetId</c> — вкладка
/// «По оборудованию» с предвыбранным объектом, <c>CategoryId</c> (без <c>AssetId</c>) — вкладка
/// «Категории». Вызовы <c>INavigationService.NavigateTo("MaintenanceNorms", args)</c> из других
/// страниц (реестр оборудования и т.п.) — вне текущего scope, только приём аргументов.
/// </summary>
public sealed record MaintenanceNormsNavigationArgs(
    Guid? AssetId = null,
    Guid? CategoryId = null);
