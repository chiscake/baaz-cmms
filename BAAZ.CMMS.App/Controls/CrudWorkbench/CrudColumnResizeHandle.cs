using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Область изменения ширины колонки с курсором ↔.</summary>
internal sealed class CrudColumnResizeHandle : UserControl
{
    public CrudColumnResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        Content = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }
}
