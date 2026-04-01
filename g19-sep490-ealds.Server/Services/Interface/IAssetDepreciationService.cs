namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetDepreciationService
{
    Task RunMonthlyDepreciation();
    Task UpdateDepreciation(int recordId, decimal newAmount);
}