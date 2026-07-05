using BAAZ.CMMS.App.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.Home;

public sealed partial class HomeStatTile : UserControl
{
    public HomeStatTile()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ApplyFromContext();
        Loaded += (_, _) => ApplyFromContext();
        ActualThemeChanged += (_, _) => ApplyFromContext();
    }

    private void ApplyFromContext()
    {
        if (DataContext is not HomeStatItem item)
        {
            return;
        }

        if (item.IsSpacer)
        {
            TileBorder.Visibility = Visibility.Visible;
            TileBorder.Background = null;
            TileBorder.BorderThickness = new Thickness(0);
            IconGlyph.Visibility = Visibility.Collapsed;
            ValueText.Visibility = Visibility.Collapsed;
            LabelText.Visibility = Visibility.Collapsed;
            NavigateChevron.Visibility = Visibility.Collapsed;
            return;
        }

        TileBorder.Visibility = Visibility.Visible;
        TileBorder.BorderThickness = new Thickness(1);
        IconGlyph.Visibility = Visibility.Visible;
        ValueText.Visibility = Visibility.Visible;
        LabelText.Visibility = Visibility.Visible;

        IconGlyph.Glyph = item.Glyph;
        ValueText.Text = item.Value;
        LabelText.Text = item.Label;

        var theme = ActualTheme;
        IconGlyph.Foreground = new SolidColorBrush(StatusBadgePalette.ResolveColor(item.IconColorToken, theme));
        ValueText.Foreground = new SolidColorBrush(StatusBadgePalette.ResolveColor(item.ValueColorToken, theme));

        if (item.IsNavigable)
        {
            TileRoot.Visibility = Visibility.Visible;
            NavigateChevron.Visibility = Visibility.Visible;
            _navigableItem = item;
        }
        else
        {
            TileRoot.Visibility = Visibility.Visible;
            NavigateChevron.Visibility = Visibility.Collapsed;
            _navigableItem = null;
        }
    }

    private HomeStatItem? _navigableItem;

    private void TileRoot_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (_navigableItem?.NavigateCommand is null || string.IsNullOrWhiteSpace(_navigableItem.PageKey))
            return;

        if (_navigableItem.NavigateCommand.CanExecute(_navigableItem))
            _navigableItem.NavigateCommand.Execute(_navigableItem);
    }
}
