namespace KoiGuardian.Models.Response;

public class AccountDashboardResponse
{
    public int TotalUser {  get; set; }

    public int TotalShop { get; set; }

    public int TotalActiveUser { get; set; }

    public int TotalInactiveUser { get; set; }

    public int TotaNotVerifiedlUser { get; set; }

    public int TotalBannedUser { get; set; }

    public int UnPreniumUser { get; set; }

    public int PreniumUser { get; set; }
}