using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class UserDTO
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public int Status { get; set; }
    
    // Using string representation of roles for simplicity in listing
    public List<string> Roles { get; set; } = new List<string>();
}
