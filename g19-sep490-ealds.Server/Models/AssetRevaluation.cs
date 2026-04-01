namespace g19_sep490_ealds.Server.Models;

public partial class AssetRevaluation
{
    public int Id { get; set; }

    public int AssetInstanceId { get; set; }

    public decimal OldValue { get; set; }

    public decimal NewValue { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string? Reason { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;
}