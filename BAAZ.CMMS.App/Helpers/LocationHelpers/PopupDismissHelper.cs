using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>Закрывает открытый Popup/Flyout-предка перед модальным ContentDialog.</summary>
public static class PopupDismissHelper
{
    public static void CloseAncestorPopups(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is Popup { IsOpen: true } popup)
            {
                popup.IsOpen = false;
                return;
            }

            element = VisualTreeHelper.GetParent(element);
        }
    }
}
