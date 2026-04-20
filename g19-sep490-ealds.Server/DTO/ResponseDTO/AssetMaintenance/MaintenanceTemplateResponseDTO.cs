using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;

public class MaintenanceTemplateResponseDTO
{
    public int TemplateId { get; set; }

    public int AssetTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public MaintenanceFrequencyType FrequencyType { get; set; }

    public int RepeatIntervalValue { get; set; }

    public MaintenanceRepeatIntervalUnit RepeatIntervalUnit { get; set; } 

    public bool IsActive { get; set; }

    public DateTime? OneTimeScheduledDate { get; set; }
}