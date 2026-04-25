namespace g19_sep490_ealds.Server.DTOs.Depreciation;

public class CreatePolicyDTO
{
    public string Name { get; set; }
    public int Method { get; set; }
    public int UsefullLifeMonths { get; set; }
    public decimal SalvageValue { get; set; }
}
