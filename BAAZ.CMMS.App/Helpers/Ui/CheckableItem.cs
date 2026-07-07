using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Строка чек-листа multi-select (напр. отделы ремонта на нормативе, UC-A5).</summary>
public sealed partial class CheckableItem : ObservableObject
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}
