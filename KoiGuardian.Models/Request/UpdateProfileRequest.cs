using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class UpdateProfileRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public AddressDto address { get; set; }
        public DateTime UserReminder { get; set; }

        public string? Avatar { get; set; } 
        public string? ShopDescription { get; set; } 
        public string? BizLicense { get; set; }
    }
}
