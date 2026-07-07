using System.ComponentModel;
using System.Windows.Input;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Контракт VM для <see cref="CrudPaginatorControl"/>.</summary>
public interface ICrudPaginationHost : INotifyPropertyChanged
{
    int CurrentPage { get; set; }

    int TotalPages { get; }

    int TotalRecords { get; }

    int PageSize { get; set; }

    bool CanGoToPreviousPage { get; }

    bool CanGoToNextPage { get; }

    string PaginationPageLabel { get; }

    string PaginationOfText { get; }

    string PaginationRecordsText { get; }

    string PaginationPageSizeText { get; }

    ICommand GoToPreviousPageCommand { get; }

    ICommand GoToNextPageCommand { get; }

    ICommand SetPageSizeCommand { get; }
}
