using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class UpdateUserDTO
{
    public int Status { get; set; }

    public List<int> RoleIds { get; set; } = new List<int>();
}
