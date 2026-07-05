using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Requester.NewRequest;

public sealed partial class NewRequestPage : Page
{
    public NewRequestViewModel ViewModel { get; }

    public NewRequestPage()
    {
        ViewModel = App.Services.GetRequiredService<NewRequestViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.InitializeAsync(e.Parameter);
    }

    private void AssetSuggest_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.OnAssetSearchTextChanged(sender.Text);
        }
    }

    private void AssetSuggest_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AssetPickerRow row)
        {
            ViewModel.SelectAssetSuggestion(row);
        }
    }

    private void AssetSuggest_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is AssetPickerRow row)
        {
            ViewModel.SelectAssetSuggestion(row);
            return;
        }

        if (ViewModel.TrySelectFirstSuggestion())
        {
            sender.Text = ViewModel.AssetSearchText;
        }
    }

    private void SubjectSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        UpdateSubjectModeFromSelector(sender);
    }

    private void UpdateSubjectModeFromSelector(SelectorBar selector)
    {
        if (selector.SelectedItem is null)
        {
            return;
        }

        var index = selector.Items.IndexOf(selector.SelectedItem);
        if (index < 0)
        {
            return;
        }

        ViewModel.SetSubjectMode(index == 0
            ? NewRequestSubjectMode.Asset
            : NewRequestSubjectMode.Location);
    }
}
