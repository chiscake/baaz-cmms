using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;

public sealed partial class MaterialRequisitionLineItemControl : UserControl
{
    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(
            nameof(Row),
            typeof(MaterialRequisitionLineRow),
            typeof(MaterialRequisitionLineItemControl),
            new PropertyMetadata(null));

    public MaterialRequisitionLineRow? Row
    {
        get => (MaterialRequisitionLineRow?)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public MaterialRequisitionLineItemControl() => InitializeComponent();
}
