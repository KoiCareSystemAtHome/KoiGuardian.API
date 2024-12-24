using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.Models.Request;

public class AccountFilterRequest
{
    public string? UserName { get; set; }
    public UserStatus? Status { get; set; }
    public bool? IsUsingPackage { get; set; }
    public string? PackageID { get; set; }
    public DateTime? StartDate { get; set; } = DateTime.Now.AddMonths(-1);
    public DateTime? EndDate { get; set; } = DateTime.Now;
}