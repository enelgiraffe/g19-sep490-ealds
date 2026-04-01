using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;

namespace g19_sep490_ealds.Server.Services.ServiceInterface;
public interface IAssetCapitalizationService
{
    Task<AssetCapitalizationResponseDTO> CapitalizeAssetAsync(
        AssetCapitalizationRequestDTO request,
        int userId,
        bool skipPurchaseRequestSideEffects = false);

    Task<AssetCapitalizationResponseDTO> CapitalizeFromPurchaseRequestAsync(AssetCapitalizationFromRequestDTO request, int userId);

    Task<CapitalizePurchaseRequestLinesResponseDTO> CapitalizePurchaseRequestLinesAsync(
        CapitalizePurchaseRequestLinesDTO request,
        int userId);
}