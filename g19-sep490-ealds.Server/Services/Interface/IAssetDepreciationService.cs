namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetDepreciationService
{
    Task RunMonthlyDepreciation(DateTime? scheduledFireTimeUtc = null);
    Task RunManualDepreciation(int? assetInstanceId, int? year, int? month);
    Task AssignPolicyAsync(int assetInstanceId, int policyId);
}