using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.LocationPicker;
using BAAZ.CMMS.App.Controls.LocationScopePicker;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using VirtualKey = global::Windows.System.VirtualKey;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Показывает inline-flyout для редактирования ячейки (текст, enum, дата, дерево локаций).
/// </summary>
public static class CrudCellEditFlyout
{
    public static void Show(
        FrameworkElement anchor,
        CrudColumnDefinition column,
        string? currentValue,
        Func<string?, Task> onSave,
        Func<string?, string?>? validate = null)
    {
        var flyout = new Flyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom,
        };

        flyout.FlyoutPresenterStyle = column.EditKind switch
        {
            CrudColumnEditKind.LocationTree or CrudColumnEditKind.LocationScopeTree => BuildLocationPresenterStyle(),
            CrudColumnEditKind.Date => BuildDatePresenterStyle(),
            _ => BuildPresenterStyle(),
        };

        flyout.Content = column.EditKind switch
        {
            CrudColumnEditKind.LocationTree =>
                BuildLocationTreeContent(column, currentValue, flyout, onSave),
            CrudColumnEditKind.LocationScopeTree =>
                BuildLocationScopeTreeContent(column, currentValue, flyout, onSave),
            CrudColumnEditKind.Date =>
                BuildDateContent(currentValue, flyout, onSave),
            CrudColumnEditKind.EnumList when column.EnumOptions is { Count: > 0 } =>
                BuildEnumContent(column, currentValue, flyout, onSave),
            _ => BuildTextContent(column, currentValue, flyout, onSave, validate),
        };

        flyout.ShowAt(anchor);
    }

    // ── Text mode ────────────────────────────────────────────────────────────

    private static UIElement BuildTextContent(
        CrudColumnDefinition col,
        string? currentValue,
        Flyout flyout,
        Func<string?, Task> onSave,
        Func<string?, string?>? validate)
    {
        var tb = new TextBox
        {
            Text = currentValue ?? string.Empty,
            MinWidth = 220,
            AcceptsReturn = false,
        };

        if (col.MaxLength is int maxLen and > 0)
            tb.MaxLength = maxLen;

        var errorBlock = new TextBlock
        {
            Foreground = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 4, 0, 0),
        };

        var hint = new TextBlock
        {
            Text = ResourceStrings.Get("CrudGrid_EditHint"),
            FontSize = 11,
            Opacity = 0.6,
            Margin = new Thickness(0, 4, 0, 0),
        };

        var saveBtn = new Button { Content = ResourceStrings.Get("CrudGrid_Save") };
        var cancelBtn = new Button
        {
            Content = ResourceStrings.Get("CrudGrid_Cancel"),
            Style = (Style)Application.Current.Resources["DefaultButtonStyle"],
        };

        saveBtn.Click += async (_, _) => await TrySaveAsync(tb, flyout, onSave, validate, errorBlock);
        cancelBtn.Click += (_, _) => flyout.Hide();

        tb.KeyDown += async (_, e) =>
        {
            if (e.Key == VirtualKey.Enter)
                await TrySaveAsync(tb, flyout, onSave, validate, errorBlock);
            else if (e.Key == VirtualKey.Escape)
                flyout.Hide();
        };

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };
        btnRow.Children.Add(saveBtn);
        btnRow.Children.Add(cancelBtn);

        var panel = new StackPanel { Spacing = 0 };
        panel.Children.Add(tb);
        panel.Children.Add(errorBlock);
        panel.Children.Add(hint);
        panel.Children.Add(btnRow);

        flyout.Opened += (_, _) =>
        {
            tb.SelectAll();
            tb.Focus(FocusState.Programmatic);
        };

        return panel;
    }

    private static async Task TrySaveAsync(
        TextBox tb,
        Flyout flyout,
        Func<string?, Task> onSave,
        Func<string?, string?>? validate,
        TextBlock errorBlock)
    {
        var trimmed = tb.Text.Trim() is string s && s.Length > 0 ? s : null;
        var error = validate?.Invoke(trimmed);
        if (!string.IsNullOrEmpty(error))
        {
            errorBlock.Text = error;
            errorBlock.Visibility = Visibility.Visible;
            return;
        }

        errorBlock.Visibility = Visibility.Collapsed;
        flyout.Hide();
        await onSave(trimmed);
    }

    // ── EnumList mode ─────────────────────────────────────────────────────────

    private static UIElement BuildEnumContent(
        CrudColumnDefinition col,
        string? currentValue,
        Flyout flyout,
        Func<string?, Task> onSave)
    {
        var list = new ListView
        {
            MinWidth = 180,
            MaxHeight = 240,
            SelectionMode = ListViewSelectionMode.Single,
            IsItemClickEnabled = true,
            ItemTemplate = CreateEnumItemTemplate(),
            ItemsSource = col.EnumOptions!.ToList(),
        };

        var isInitializing = true;
        var isCommitting = false;

        async Task CommitAsync(CrudEnumOption opt)
        {
            if (isCommitting)
                return;

            isCommitting = true;
            try
            {
                flyout.Hide();
                await onSave(opt.Value);
            }
            finally
            {
                isCommitting = false;
            }
        }

        list.SelectionChanged += async (_, _) =>
        {
            if (isInitializing)
                return;

            if (list.SelectedItem is CrudEnumOption opt)
                await CommitAsync(opt);
        };

        list.ItemClick += async (_, e) =>
        {
            if (isInitializing)
                return;

            if (e.ClickedItem is CrudEnumOption opt)
                await CommitAsync(opt);
        };

        list.KeyDown += async (_, e) =>
        {
            if (e.Key == VirtualKey.Enter && list.SelectedItem is CrudEnumOption opt)
                await CommitAsync(opt);
            else if (e.Key == VirtualKey.Escape)
                flyout.Hide();
        };

        var current = col.EnumOptions!.FirstOrDefault(o => o.Value == currentValue);
        if (current is not null)
            list.SelectedItem = current;

        isInitializing = false;

        flyout.Opened += (_, _) => list.Focus(FocusState.Programmatic);

        return list;
    }

    private static DataTemplate CreateEnumItemTemplate() =>
        (DataTemplate)XamlReader.Load(
            """
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <TextBlock Text="{Binding Label}" Padding="8,6" />
            </DataTemplate>
            """);

    // ── Date mode (CalendarView — календарь сразу в flyout, без вложенного popup) ──

    private static UIElement BuildDateContent(
        string? currentValue,
        Flyout flyout,
        Func<string?, Task> onSave)
    {
        var currentDate = DateDisplayHelper.ParseWireFormat(currentValue);
        var isInitializing = true;
        var isCommitting = false;

        var calendar = new CalendarView
        {
            SelectionMode = CalendarViewSelectionMode.Single,
            IsTodayHighlighted = true,
            IsOutOfScopeEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        if (currentDate.HasValue)
        {
            var dt = currentDate.Value.ToDateTime(TimeOnly.MinValue);
            calendar.SetDisplayDate(dt);
            calendar.SelectedDates.Add(dt);
        }
        else
        {
            calendar.SetDisplayDate(DateTime.Today);
        }

        async Task CommitAsync(string? wireValue)
        {
            if (isCommitting)
                return;

            if (string.Equals(wireValue, currentValue, StringComparison.Ordinal))
            {
                flyout.Hide();
                return;
            }

            isCommitting = true;
            try
            {
                flyout.Hide();
                await onSave(wireValue);
            }
            finally
            {
                isCommitting = false;
            }
        }

        calendar.SelectedDatesChanged += async (_, e) =>
        {
            if (isInitializing)
                return;

            if (e.AddedDates.Count > 0)
            {
                var picked = DateOnly.FromDateTime(e.AddedDates[0].Date);
                await CommitAsync(DateDisplayHelper.ToWireFormat(picked));
            }
        };

        calendar.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape)
                flyout.Hide();
        };

        var clearBtn = new Button
        {
            Content = ResourceStrings.Get("CrudGrid_ClearDate"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0),
            Style = (Style)Application.Current.Resources["DefaultButtonStyle"],
        };
        clearBtn.Click += async (_, _) => await CommitAsync(null);

        isInitializing = false;

        var panel = new StackPanel();
        panel.Children.Add(calendar);
        panel.Children.Add(clearBtn);

        flyout.Opened += (_, _) => calendar.Focus(FocusState.Programmatic);

        return panel;
    }

    // ── Location tree mode ────────────────────────────────────────────────────

    private static UIElement BuildLocationTreeContent(
        CrudColumnDefinition col,
        string? currentValue,
        Flyout flyout,
        Func<string?, Task> onSave)
    {
        Guid? currentId = Guid.TryParse(currentValue, out var parsedId) ? parsedId : null;
        var isCommitting = false;

        var picker = new LocationPicker.LocationPicker
        {
            MinWidth = 360,
            DisabledNodeIds = col.DisabledLocationNodeIds,
            AllowClearSelection = col.AllowClearLocationSelection,
            ClearParentLabel = ResourceStrings.Get("Locations_Editor_ClearParent"),
            IsFlyoutHost = true,
            ShowDialogButton = true,
            SearchPlaceholder = ResourceStrings.Get("LocationPicker_SearchPlaceholder"),
            NoResultsMessage = ResourceStrings.Get("LocationPicker_SearchNoResults"),
            ClearSearchLabel = ResourceStrings.Get("LocationPicker_ClearSearch"),
        };

        if (col.LocationTreeVersion > 0)
        {
            picker.AttachTree(
                col.LocationTreeRoots ?? [],
                col.LocationPaths ?? new Dictionary<Guid, string>(),
                col.LocationTreeVersion,
                "inline-flyout");
        }
        else
        {
            picker.TreeItems = col.LocationTreeRoots ?? [];
            picker.LocationPaths = col.LocationPaths;
        }

        async Task CommitAsync(Guid? locationId)
        {
            if (isCommitting)
                return;

            var newValue = locationId?.ToString();
            if (string.Equals(newValue, currentValue, StringComparison.Ordinal))
            {
                flyout.Hide();
                return;
            }

            isCommitting = true;
            try
            {
                flyout.Hide();
                await onSave(newValue);
            }
            finally
            {
                isCommitting = false;
            }
        }

        picker.LocationSelected += async (_, locationId) =>
        {
            await CommitAsync(locationId);
        };

        picker.KeyDown += async (_, e) =>
        {
            if (e.Key == VirtualKey.Escape)
                flyout.Hide();
        };

        flyout.Opened += (_, _) =>
        {
            picker.SetSelection(currentId, "inline-flyout");
            picker.Focus(FocusState.Programmatic);
        };

        return picker;
    }

    // ── Location scope tree mode (multi-select) ───────────────────────────────

    private static UIElement BuildLocationScopeTreeContent(
        CrudColumnDefinition col,
        string? currentValue,
        Flyout flyout,
        Func<string?, Task> onSave)
    {
        var currentIds = LocationScopeIdsWireFormat.Parse(currentValue);
        var isCommitting = false;

        // Используем кэшированную проекцию из колонки (построена при загрузке данных).
        var projection = col.ScopeTreeProjection
            ?? (col.LocationTreeVersion > 0
                ? LocationScopeTreeProjection.Build(col.LocationTreeRoots ?? [], col.LocationTreeVersion)
                : LocationScopeTreeProjection.Empty);

        var currentExpanded = projection.NodesById.Count > 0
            ? LocationScopeSelectionHelper.ExpandAnchorsToSelection(currentIds, projection.NodesById)
            : currentIds;

        var picker = new LocationScopePicker.LocationScopePicker
        {
            MinWidth = 360,
            MaxWidth = 360,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsFlyoutHost = true,
            ShowDialogButton = true,
            SearchPlaceholder = ResourceStrings.Get("LocationPicker_SearchPlaceholder"),
            NoResultsMessage = ResourceStrings.Get("LocationPicker_SearchNoResults"),
            ClearSearchLabel = ResourceStrings.Get("LocationPicker_ClearSearch"),
        };

        // AttachTreeSync: строит дерево синхронно — без ScheduleRebuild и freeze.
        var roots = col.LocationTreeRoots ?? [];
        picker.AttachTreeSync(
            projection,
            roots,
            col.LocationPaths ?? new Dictionary<Guid, string>(),
            col.LocationTreeVersion);
        picker.SetSelection(currentIds);

        async Task CommitAsync(IReadOnlySet<Guid> selectedIds)
        {
            if (isCommitting)
                return;

            if (currentExpanded.SetEquals(selectedIds))
            {
                flyout.Hide();
                return;
            }

            var newValue = selectedIds.Count > 0
                ? LocationScopeIdsWireFormat.Serialize(selectedIds)
                : null;

            isCommitting = true;
            try
            {
                flyout.Hide();
                await onSave(newValue);
            }
            finally
            {
                isCommitting = false;
            }
        }

        var saveBtn = new Button
        {
            Content = ResourceStrings.Get("CrudGrid_Save"),
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
        };
        var cancelBtn = new Button
        {
            Content = ResourceStrings.Get("CrudGrid_Cancel"),
            Style = (Style)Application.Current.Resources["DefaultButtonStyle"],
        };

        saveBtn.Click += async (_, _) =>
        {
            var selected = picker.SelectedLocationIds ?? new HashSet<Guid>();
            await CommitAsync(selected);
        };

        cancelBtn.Click += (_, _) => flyout.Hide();

        picker.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape)
                flyout.Hide();
        };

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };
        btnRow.Children.Add(saveBtn);
        btnRow.Children.Add(cancelBtn);

        var panel = new StackPanel();
        panel.Children.Add(picker);
        panel.Children.Add(btnRow);

        flyout.Opened += (_, _) => picker.Focus(FocusState.Programmatic);

        return panel;
    }

    // ── Presenter style ───────────────────────────────────────────────────────

    private static Style BuildPresenterStyle()
    {
        var style = new Style(typeof(FlyoutPresenter));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 180.0));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12)));
        return style;
    }

    private static Style BuildLocationPresenterStyle()
    {
        var style = new Style(typeof(FlyoutPresenter));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 360.0));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12)));
        return style;
    }

    private static Style BuildDatePresenterStyle()
    {
        var style = new Style(typeof(FlyoutPresenter));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 320.0));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8)));
        return style;
    }
}
