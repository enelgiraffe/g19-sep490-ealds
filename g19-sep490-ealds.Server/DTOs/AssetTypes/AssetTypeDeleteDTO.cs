using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.AssetTypes;

public class AssetTypeDeleteDTO
{
    [Required]
    public int AssetTypeId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
}

