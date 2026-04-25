
namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetTypeService
{
    public Task<IEnumerable<AssetTypeResponseDTO>> GetAllAssetTypeAsync();
    public Task<IEnumerable<AssetTypeResponseDTO>> SearchAssetTypeByKeyAsync(string name);
    public Task<AssetTypeResponseDTO> UpdateAssetTypeAsync(int id, AssetTypeUpdateDTO update);
    public Task<AssetTypeResponseDTO> CreateAssetTypeAsync(AssetTypeCreateDTO create);
    public Task<bool> DeleteAssetTypeAsync(int id);
}
