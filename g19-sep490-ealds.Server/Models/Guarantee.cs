namespace g19_sep490_ealds.Server.Models;

public partial class Guarantee
{
    public int GuaranteeId { get; set; }

    public int AssetInstanceId { get; set; }

    public int WarrantyPeriodValue { get; set; }

    public string WarrantyPeriodUnit { get; set; } = null!;

    public string? WarrantyConditions { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly WarrantyEndDate { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;
}