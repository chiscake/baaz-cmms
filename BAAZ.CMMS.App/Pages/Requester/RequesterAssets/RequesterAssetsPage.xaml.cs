using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Requester.RequesterAssets;

public sealed partial class RequesterAssetsPage : Page
{
    public RequesterAssetsViewModel ViewModel { get; }

    public RequesterAssetsPage()
    {
        ViewModel = App.Services.GetRequiredService<RequesterAssetsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.OnPageLoadedAsync();
    }

    private void AssetList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RequesterAssetListItem item)
        {
            ViewModel.OpenNewRequestForAsset(item.Id);
        }
    }
}
