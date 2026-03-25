namespace g19_sep490_ealds.Server.Utils;

public static class DepreciationFormula
{
    // khấu hao đưuòng thẳng
    public static decimal CalculateStraightLine(
        decimal cost,
        decimal salvage,
        int usefulLifeMonths)
    {
        if (usefulLifeMonths <= 0) return 0;

        return (cost - salvage) / usefulLifeMonths;
    }
}
