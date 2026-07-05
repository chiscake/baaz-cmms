using System;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Controls.LocationScopePicker;

/// <summary>
/// Узел дерева зон заявок. CheckedState — observable-свойство:
/// true = весь поддерево выделен, null = частично, false = не выделен.
/// Привязывается напрямую к CheckBox.IsChecked (OneWay) без побочных событий.
/// </summary>
public sealed partial class LocationScopeNode : ObservableObject
{
    [ObservableProperty]
    public partial bool? CheckedState { get; set; } = false;
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public bool IsEnabled { get; init; } = true;

    public IList<LocationScopeNode> Children { get; init; } = [];
}
