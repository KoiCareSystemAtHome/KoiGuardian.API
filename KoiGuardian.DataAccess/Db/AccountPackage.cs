using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class AccountPackage
    {
        public Guid AccountPackageid { get; set; }
        public string AccountId { get; set; }
        public Guid PackageId { get; set; }
        public DateTime PurchaseDate { get; set; }

        public virtual Package Package { get; set; }
        public virtual User Member { get; set; }
    }
}
