using Microsoft.AspNetCore.Identity;
using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.DataAccess.Db;

public class User : IdentityUser
{
    public string PackageId { get; set; } = string.Empty;

    public UserStatus Status { get; set; }

    public string Avatar { get; set; } = string.Empty;

    public int Code { get; set; } = 0;

    public DateTime? CreatedDate { get; set; } 

    public DateTime? ValidUntil { get; set; } 
}