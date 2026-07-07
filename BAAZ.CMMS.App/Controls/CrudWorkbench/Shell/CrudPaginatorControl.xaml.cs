using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudPaginatorControl : UserControl
{
    private static readonly int[] PageSizeOptions = [100, 500, 1000];

    public CrudPaginatorControl()
    {
        InitializeComponent();
    }

    private void PageSizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ICrudPaginationHost host) return;

        var flyout = new MenuFlyout();
        foreach (var size in PageSizeOptions)
        {
            var item = new MenuFlyoutItem
            {
                Text = string.Format(ResourceStrings.Get("CrudGrid_Pagination_RowsFormat"), size),
                Tag = size,
                FontWeight = host.PageSize == size
                    ? Microsoft.UI.Text.FontWeights.SemiBold
                    : Microsoft.UI.Text.FontWeights.Normal,
            };
            item.Click += PageSizeItem_Click;
            flyout.Items.Add(item);
        }

        flyout.ShowAt(PageSizeButton);
    }

    private void PageSizeItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not int size) return;
        if (DataContext is not ICrudPaginationHost host) return;
        if (host.SetPageSizeCommand.CanExecute(size))
            host.SetPageSizeCommand.Execute(size);
    }
}
