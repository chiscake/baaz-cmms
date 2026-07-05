using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;

public sealed partial class MaterialRequisitionLineRow : ObservableObject
{
    public MaterialRequisitionViewModel? Owner { get; internal set; }

    [ObservableProperty]
    public partial int LineNumber { get; set; } = 1;

    public bool ShowRemoveButton => Owner is null || Owner.Lines.Count > 1;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Sku { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial string Unit { get; set; } = "шт";

    [ObservableProperty]
    public partial string LineNote { get; set; } = string.Empty;

    internal void NotifyRemoveVisibility() => OnPropertyChanged(nameof(ShowRemoveButton));
}