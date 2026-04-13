namespace g19_sep490_ealds.Server.Models;

public partial class AssetCapitalization
{
    public int Id { get; set; }

    public int AssetInstanceId { get; set; }

    public DateTime CapitalizedDate { get; set; }

    public int CapitalizedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual User CapitalizedByNavigation { get; set; } = null!;
}