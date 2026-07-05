using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Requester.NewRequest;
using BAAZ.CMMS.Core.Services.Catalog;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Requester.RequesterAssets;

public sealed partial class RequesterAssetsViewModel : PageViewModelBase
{
    private readonly IRequesterAssetCatalog _requesterAssetCatalog;
    private readonly INavigationService _navigationService;

    private List<RequesterAssetListItem> _allItems = [];

    public RequesterAssetsViewModel(
        IRequesterAssetCatalog requesterAssetCatalog,
        INavigationService navigationService)
    {
        _requesterAssetCatalog = requesterAssetCatalog;
        _navigationService = navigationService;
        FilteredItems = [];
    }

    public override string PageTitle => ResourceStrings.Get("Nav_RequesterAssets");

    public string NewRequestLabel => ResourceStrings.Get("MyRequests_NewRequest");

    public string SearchPlaceholder => ResourceStrings.Get("RequesterAssets_Search_Placeholder");

    public string EmptyListText => ResourceStrings.Get("RequesterAssets_Empty_List");

    public string EmptyFilterText => ResourceStrings.Get("RequesterAssets_Empty_Filter");

    public ObservableCollection<RequesterAssetListItem> FilteredItems { get; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasItems { get; set; }

    public bool ShowEmptyList => !IsLoading && !HasItems;

    public bool ShowEmptyFilter => !IsLoading && HasItems && FilteredItems.Count == 0;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyList));
        OnPropertyChanged(nameof(ShowEmptyFilter));
    }

    partial void OnHasItemsChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyList));
        OnPropertyChanged(nameof(ShowEmptyFilter));
    }

    public async Task OnPageLoadedAsync()
    {
        if (_allItems.Count > 0)
        {
            return;
        }

        IsLoading = true;
        InfoBanner.Report(string.Empty);

        try
        {
            var result = await _requesterAssetCatalog.GetActiveScopedAssetsAsync();
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
                return;
            }

            _allItems = result.Value!
                .Select(MapItem)
                .OrderBy(a => a.DisplayLine, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            HasItems = _allItems.Count > 0;
            ApplyFilter();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void OpenNewRequestForAsset(Guid assetId) =>
        _navigationService.NavigateTo("NewRequest", new NewRequestNavigationArgs(assetId));

    [RelayCommand]
    private void OpenNewRequest() => _navigationService.NavigateTo("NewRequest");

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        FilteredItems.Clear();

        IEnumerable<RequesterAssetListItem> source = _allItems;
        if (!string.IsNullOrEmpty(query))
        {
            source = source.Where(item => item.MatchesSearch(query));
        }

        foreach (var item in source)
        {
            FilteredItems.Add(item);
        }

        OnPropertyChanged(nameof(ShowEmptyList));
        OnPropertyChanged(nameof(ShowEmptyFilter));
    }

    private static RequesterAssetListItem MapItem(RequesterScopedAssetItem item)
    {
        var asset = item.Asset;
        var locationPath = item.LocationFullPath;
        if (locationPath is null && !string.IsNullOrWhiteSpace(asset.LocationName))
            locationPath = asset.LocationName;

        return new RequesterAssetListItem
        {
            Id = asset.Id,
            DisplayLine = $"{asset.AssetNumber} — {asset.Name}",
            LocationPath = locationPath,
        };
    }
}

public sealed class RequesterAssetListItem
{
    public Guid Id { get; init; }

    public required string DisplayLine { get; init; }

    public string? LocationPath { get; init; }

    public bool MatchesSearch(string query) =>
        DisplayLine.Contains(query, StringComparison.CurrentCultureIgnoreCase)
        || (LocationPath?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false);
}
