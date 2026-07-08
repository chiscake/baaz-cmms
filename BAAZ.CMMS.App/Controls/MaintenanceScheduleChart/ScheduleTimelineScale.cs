using System;
using System.Collections.Generic;
using System.Globalization;

using BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

namespace BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;

public sealed class ScheduleTimelineScale
{
    public ScheduleTimelineScale(ScheduleZoomPreset preset, DateOnly anchorToday)
    {
        Preset = preset;
        AnchorToday = anchorToday;
        (RangeStart, RangeEnd) = ComputeDefaultRange(preset, anchorToday);
        DayWidth = preset switch
        {
            ScheduleZoomPreset.Week => 64,
            ScheduleZoomPreset.Quarter => 16,
            _ => 32,
        };
    }

    public ScheduleZoomPreset Preset { get; private set; }

    public DateOnly AnchorToday { get; }

    public DateOnly RangeStart { get; private set; }

    public DateOnly RangeEnd { get; private set; }

    public double DayWidth { get; private set; }

    public int DayCount => RangeEnd.DayNumber - RangeStart.DayNumber + 1;

    public double TotalWidth => DayCount * DayWidth;

    public void ResetToDefault()
    {
        (RangeStart, RangeEnd) = ComputeDefaultRange(Preset, AnchorToday);
    }

    public void SetPreset(ScheduleZoomPreset preset)
    {
        Preset = preset;
        DayWidth = preset switch
        {
            ScheduleZoomPreset.Week => 64,
            ScheduleZoomPreset.Quarter => 16,
            _ => 32,
        };
        ResetToDefault();
    }

    public void NavigatePrevious()
    {
        var shift = Preset switch
        {
            ScheduleZoomPreset.Week => 7,
            ScheduleZoomPreset.Quarter => 30,
            _ => 14,
        };
        RangeStart = RangeStart.AddDays(-shift);
        RangeEnd = RangeEnd.AddDays(-shift);
    }

    public void NavigateNext()
    {
        var shift = Preset switch
        {
            ScheduleZoomPreset.Week => 7,
            ScheduleZoomPreset.Quarter => 30,
            _ => 14,
        };
        RangeStart = RangeStart.AddDays(shift);
        RangeEnd = RangeEnd.AddDays(shift);
    }

    public void SetVisibleRange(DateOnly rangeStart, DateOnly rangeEnd)
    {
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
    }

    public double ToX(DateOnly date)
    {
        var index = date.DayNumber - RangeStart.DayNumber;
        return index * DayWidth + DayWidth / 2;
    }

    public bool Contains(DateOnly date) =>
        date >= RangeStart && date <= RangeEnd;

    public IReadOnlyList<ChartDayHeaderVm> BuildDayHeaders()
    {
        var list = new List<ChartDayHeaderVm>();
        string? currentMonth = null;
        for (var d = RangeStart; d <= RangeEnd; d = d.AddDays(1))
        {
            var monthLabel = d.Day == 1 || currentMonth is null
                ? d.ToString("MMMM yyyy", CultureInfo.CurrentCulture)
                : null;
            if (monthLabel is not null)
                currentMonth = monthLabel;

            var dow = d.DayOfWeek;
            list.Add(new ChartDayHeaderVm
            {
                Date = d,
                DayLabel = d.ToString("dd", CultureInfo.CurrentCulture),
                MonthLabel = monthLabel,
                IsWeekend = dow is DayOfWeek.Saturday or DayOfWeek.Sunday,
                IsToday = d == AnchorToday,
                Left = (d.DayNumber - RangeStart.DayNumber) * DayWidth,
                Width = DayWidth,
            });
        }

        return list;
    }

    public static (DateOnly Start, DateOnly End) ComputeDefaultRange(ScheduleZoomPreset preset, DateOnly today)
    {
        return preset switch
        {
            ScheduleZoomPreset.Week => ComputeWeekRange(today),
            ScheduleZoomPreset.Quarter => (today.AddDays(-13), today.AddDays(77)),
            _ => (today.AddDays(-6), today.AddDays(24)),
        };
    }

    private static (DateOnly Start, DateOnly End) ComputeWeekRange(DateOnly today)
    {
        var dayOfWeek = (int)today.DayOfWeek;
        var mondayOffset = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
        var monday = today.AddDays(mondayOffset);
        return (monday, monday.AddDays(6));
    }
}
