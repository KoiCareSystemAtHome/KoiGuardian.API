﻿using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using static KoiGuardian.Models.Enums.CommonEnums;

namespace KoiGuardian.DataAccess.Db;

public class User : IdentityUser
{
    public Guid? PackageId { get; set; } 

    public UserStatus Status { get; set; }

    public int Code { get; set; } = 0;

    public DateTime? CreatedDate { get; set; } 

    public DateTime? ValidUntil { get; set; }
    public TimeOnly? UserReminder { get; set; }

    [JsonIgnore]
    public virtual Wallet Wallet { get; set; }
    [JsonIgnore]
    public virtual Member Member { get; set; }
}