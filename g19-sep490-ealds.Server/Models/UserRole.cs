using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Keyless]
[Table("UserRole")]
public partial class UserRole
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
