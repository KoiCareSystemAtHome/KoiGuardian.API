using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Wallet
    {
        [Key]
        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public float Amount { get; set; }
        public string Status { get; set; }

        public virtual User User { get; set; }

    }
}
