using g19_sep490_ealds.Server.Models.DTO.RequestDTO;
using g19_sep490_ealds.Server.Models.DTO.ResponseDTO;

namespace g19_sep490_ealds.Server.Services.ServiceInterface;
public interface IAssetCapitalizationService
{
    Task<AssetCapitalizationResponseDTO> CapitalizeAssetAsync(AssetCapitalizationRequestDTO request, int userName);
}