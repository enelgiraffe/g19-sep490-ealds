namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetRevaluationService
{
    Task RevaluateAsync(int assetInstanceId, decimal newValue);
}