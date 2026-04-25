using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.AssetTypes;

public class AssetTypeCreateDTO
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
}

