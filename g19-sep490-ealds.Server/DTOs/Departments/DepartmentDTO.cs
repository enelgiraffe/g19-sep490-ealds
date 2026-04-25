using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Departments;

public class DepartmentDTO
{
    public int DepartmentId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}

public class CreateDepartmentDTO
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    public int Status { get; set; } = 1;
}

public class UpdateDepartmentDTO
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    public int Status { get; set; }
}
