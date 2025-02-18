using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Transaction
    {
        [Key]
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string VnPayTransactionid { get; set; }
        public string UserId { get; set; }
        public Guid DocNo { get; set; }

        public virtual User User { get; set; }
    }
}
