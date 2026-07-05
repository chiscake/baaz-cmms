using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed partial class ToolRequisitionLineItemControl : UserControl
{
    public ToolRequisitionLineRow? Row
    {
        get => (ToolRequisitionLineRow?)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(nameof(Row), typeof(ToolRequisitionLineRow), typeof(ToolRequisitionLineItemControl), new PropertyMetadata(null));

    public ToolRequisitionLineItemControl()
    {
        InitializeComponent();
    }
}
