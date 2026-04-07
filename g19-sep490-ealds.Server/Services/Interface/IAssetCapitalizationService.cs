using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;

namespace g19_sep490_ealds.Server.Services.ServiceInterface;

public interface IAssetCapitalizationService
{
    Task<AssetCapitalizationResponseDTO> CapitalizeAssetAsync(AssetCapitalizationRequestDTO request, int userId);
}
