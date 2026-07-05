using System;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed partial class ToolRequisitionLineRow : ObservableObject
{
    public ToolRequisitionViewModel? Owner { get; internal set; }

    [ObservableProperty]
    public partial int LineNumber { get; set; } = 1;

    public bool ShowRemoveButton => Owner is null || Owner.Lines.Count > 1;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InventoryNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial string LineNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TmsToolCatalogItem? SelectedCatalogTool { get; set; }

    public Guid? ToolId { get; private set; }

    partial void OnSelectedCatalogToolChanged(TmsToolCatalogItem? value)
    {
        if (value is null)
        {
            ToolId = null;
            return;
        }

        ToolId = value.ToolId;
        Name = value.Name;
    }

    internal void NotifyRemoveVisibility() => OnPropertyChanged(nameof(ShowRemoveButton));
}
