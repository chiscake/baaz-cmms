using System;

using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed class CrudRowEventArgs : EventArgs
{
    public required ICrudGridRow Row { get; init; }
    public FrameworkElement? Source { get; init; }
}

public sealed class CrudCellContextEventArgs : EventArgs
{
    public required ICrudGridRow Row { get; init; }
    public required string ColumnKey { get; init; }
    public required FrameworkElement CellElement { get; init; }
}

public sealed class CrudHeaderContextEventArgs : EventArgs
{
    public required string ColumnKey { get; init; }
    public required FrameworkElement HeaderElement { get; init; }
}

public sealed class CrudSortRequestedEventArgs : EventArgs
{
    public required string ColumnKey { get; init; }
}

public sealed class CrudFilterByValueEventArgs : EventArgs
{
    public required string ColumnKey { get; init; }
    public required string? Value { get; init; }
}

public sealed class CrudCellEditEventArgs : EventArgs
{
    public required ICrudGridRow Row { get; init; }
    public required string ColumnKey { get; init; }
    public required string? OldValue { get; init; }
    public required string? NewValue { get; init; }
}
