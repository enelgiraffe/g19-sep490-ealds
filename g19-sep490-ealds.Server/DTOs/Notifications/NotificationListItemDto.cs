namespace g19_sep490_ealds.Server.DTOs.Notifications;

public class NotificationListItemDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int? RefId { get; set; }
    public DateTime SentDate { get; set; }
    public bool IsSend { get; set; }
}
