using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class WalletWithdraw
    {
        [Key]
        public Guid AccountPackageId { get; set; }
        public string UserId { get; set; }
        public Guid PackageId { get; set; } 
        public int Code { get; set; }   
        public string Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public float Money { get; set; }

        public virtual User User { get; set; }
    }
}
