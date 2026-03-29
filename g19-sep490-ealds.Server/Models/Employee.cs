using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int? UserId { get; set; }

    public int DepartmentId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateOnly? Dob { get; set; }

    public int? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public int Status { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<AssetUsage> AssetUsages { get; set; } = new List<AssetUsage>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual User? User { get; set; }
}
