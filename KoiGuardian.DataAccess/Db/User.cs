using Microsoft.AspNetCore.Identity;

namespace KoiGuardian.DataAccess.Db;

public class User : IdentityUser
{
    public string PackageId { get; set; } = string.Empty;

    public bool IsActivate { get; set; }

    public string Avatar { get; set; } = string.Empty;

}
