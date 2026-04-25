using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.AssetTypes;

public class CreateAssetTypeDto
{
    [Required(ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Asset type name is required.")]
    [StringLength(255, ErrorMessage = "Asset type name must not exceed 255 characters.")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateAssetTypeDto
{
    [Required(ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Asset type name is required.")]
    [StringLength(255, ErrorMessage = "Asset type name must not exceed 255 characters.")]
    public string Name { get; set; } = string.Empty;
}
