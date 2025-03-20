using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class WalletResponse
    {
        public Guid WalletId { get; set; }
        public string UserId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public float Amount { get; set; }
        public string Status { get; set; }
    }
}
