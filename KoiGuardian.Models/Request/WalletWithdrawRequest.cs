using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
  
        // Request DTO for creating a wallet withdrawal
        public class CreateWalletWithdrawRequest
        {
            public string UserId { get; set; }
            public float Amount { get; set; }
        }

        // Request DTO for updating a wallet withdrawal
        public class UpdateWalletWithdrawRequest
        {
            public string Status { get; set; }
        }
    
}
