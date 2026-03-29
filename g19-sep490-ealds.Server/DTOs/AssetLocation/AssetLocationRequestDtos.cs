using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.AssetLocation;

public class CreateAssetLocationDto
{
    [Required(ErrorMessage = "AssetInstanceId is required.")]
    public int AssetInstanceId { get; set; }

    [Required(ErrorMessage = "DepartmentId is required.")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "StartDate is required.")]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    [StringLength(255, ErrorMessage = "Note must not exceed 255 characters.")]
    public string? Note { get; set; }
}

public class UpdateAssetLocationDto
{
    [Required(ErrorMessage = "DepartmentId is required.")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "StartDate is required.")]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    [StringLength(255, ErrorMessage = "Note must not exceed 255 characters.")]
    public string? Note { get; set; }
}
