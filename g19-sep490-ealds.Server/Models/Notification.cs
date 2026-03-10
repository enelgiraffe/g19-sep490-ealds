using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Notification")]
public partial class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [StringLength(255)]
    public string Title { get; set; } = null!;

    [StringLength(100)]
    public string? Content { get; set; }

    public int? RefId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SentDate { get; set; }

    public bool IsSend { get; set; }
}
