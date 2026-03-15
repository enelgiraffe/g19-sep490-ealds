using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.AssetCategory;

public class CreateAssetCategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(255, ErrorMessage = "Category name must not exceed 255 characters.")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateAssetCategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(255, ErrorMessage = "Category name must not exceed 255 characters.")]
    public string Name { get; set; } = string.Empty;
}
