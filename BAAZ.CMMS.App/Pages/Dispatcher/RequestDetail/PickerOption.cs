using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;

/// <summary>Элемент ComboBox для выбора техника или ремонтного отдела.</summary>
public sealed class PickerOption
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public override string ToString() => Name;
}
