using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string VnPayTransactionId { get; set; }
    }
}
