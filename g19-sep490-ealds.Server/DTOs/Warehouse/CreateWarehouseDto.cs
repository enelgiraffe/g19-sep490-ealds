namespace g19_sep490_ealds.Server.DTOs.Warehouse;

public sealed class CreateWarehouseDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Mapped to <see cref="Models.Warehouse.Location"/> (địa điểm kho).</summary>
    public string? Location { get; set; }
}
