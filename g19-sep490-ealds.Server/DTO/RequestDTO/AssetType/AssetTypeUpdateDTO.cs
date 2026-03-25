using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTO.RequestDTO.AssetType;

public class AssetTypeUpdateDTO
{
    [Required]
    public int AssetTypeId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
}

