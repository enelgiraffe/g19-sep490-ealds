namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetDepreciationService
{
    Task RunMonthlyDepreciation();
    Task RunManualDepreciation(int? assetInstanceId, int? year, int? month);
    Task RecalculateFromPeriod(int assetInstanceId, int year, int month);
    Task UpdateDepreciation(int recordId, decimal newAmount);
    Task AssignPolicyAsync(int assetInstanceId, int policyId);
}