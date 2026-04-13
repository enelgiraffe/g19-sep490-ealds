using g19_sep490_ealds.Server.Utils.EnumsStatus;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class MaintenanceTemplate
{
    public int TemplateId { get; set; }

    public int AssetTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int FrequencyType { get; set; }

    public int RepeatIntervalValue { get; set; }

    public string RepeatIntervalUnit { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    [NotMapped]
    public MaintenanceFrequencyType FrequencyTypeEnum
    {
        get => (MaintenanceFrequencyType)FrequencyType;
        set => FrequencyType = (int)value;
    }

    [NotMapped]
    public MaintenanceRepeatIntervalUnit RepeatIntervalUnitEnum
    {
        get => Enum.Parse<MaintenanceRepeatIntervalUnit>(RepeatIntervalUnit);
        set => RepeatIntervalUnit = value.ToString();
    }
}