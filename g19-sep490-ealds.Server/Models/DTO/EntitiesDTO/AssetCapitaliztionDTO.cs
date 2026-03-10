using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Models.DTO.EntitiesDTO;

public class AssetCapitaliztionDTO 
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public DateTime CapitalizedDate { get; set; }

    public int? CapitalizedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreateDate { get; set; }
}