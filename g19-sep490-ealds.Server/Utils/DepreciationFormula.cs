namespace g19_sep490_ealds.Server.Utils;

public static class DepreciationFormula
{
    // khấu hao đường thẳng
    public static decimal CalculateStraightLine(
        decimal cost,
        decimal salvage,
        int usefulLifeMonths)
    {
        if (usefulLifeMonths <= 0)
            return 0;

        var depreciableBase = cost - salvage;
        if (depreciableBase <= 0)
            return 0;

        return Math.Round(depreciableBase / usefulLifeMonths, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ClampFinalPeriodAmount(
        decimal openingValue,
        decimal salvage,
        decimal calculatedAmount)
    {
        // Giới hạn mức khấu hao để không vượt ngưỡng giá trị thu hồi.
        var maxAllowed = openingValue - salvage;
        if (maxAllowed <= 0)
            return 0;

        return Math.Min(calculatedAmount, maxAllowed);
    }
}