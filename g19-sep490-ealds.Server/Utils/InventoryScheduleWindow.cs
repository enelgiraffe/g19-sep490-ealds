namespace g19_sep490_ealds.Server.Utils;

/// <summary>
/// Inventory sessions are defined by calendar-style windows. Comparing only UTC instants breaks
/// same-day schedules (start == end at 00:00:00Z) because the window is empty after midnight.
/// </summary>
public static class InventoryScheduleWindow
{
    public static DateTime UtcCalendarDay(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).Date;

    /// <summary>
    /// True if <paramref name="utcNow"/>'s UTC calendar day lies within the inclusive UTC calendar range of start/end.
    /// </summary>
    public static bool UtcCalendarDayInInclusiveRange(DateTime startUtc, DateTime endUtc, DateTime utcNow)
    {
        var sd = UtcCalendarDay(startUtc);
        var ed = UtcCalendarDay(endUtc);
        var nd = UtcCalendarDay(utcNow);
        return nd >= sd && nd <= ed;
    }

    /// <summary>True if <paramref name="utcNow"/>'s UTC calendar day is strictly after the end window's day (deadline has passed).</summary>
    public static bool UtcCalendarDayIsAfterEndWindow(DateTime endUtc, DateTime utcNow) =>
        UtcCalendarDay(utcNow) > UtcCalendarDay(endUtc);

    /// <summary>
    /// Whether two [start, end] ranges overlap when interpreted by UTC calendar day (inclusive).
    /// </summary>
    public static bool CalendarRangesOverlap(DateTime aStartUtc, DateTime aEndUtc, DateTime bStartUtc, DateTime bEndUtc)
    {
        var a0 = UtcCalendarDay(aStartUtc);
        var a1 = UtcCalendarDay(aEndUtc);
        var b0 = UtcCalendarDay(bStartUtc);
        var b1 = UtcCalendarDay(bEndUtc);
        return a0 <= b1 && b0 <= a1;
    }
}
