using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public int? RefId { get; set; }

    public DateTime SentDate { get; set; }

    public bool IsSend { get; set; }
}
