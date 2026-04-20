using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;

public class TemplateCreateDTO
{

    public int AssetTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public MaintenanceFrequencyType FrequencyType { get; set; }

    public int RepeatIntervalValue { get; set; }

    public MaintenanceRepeatIntervalUnit RepeatIntervalUnit { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Khi <see cref="FrequencyType"/> là một lần: ngày cần bảo dưỡng.</summary>
    public DateTime? OneTimeScheduledDate { get; set; }
}