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
        public decimal Amount { get; set; }
    }

    public class RevenueSummaryDto
    {
        public int PackageTransactionCount { get; set; } // Số giao dịch mua package
        public decimal PackageRevenue { get; set; } // Doanh thu từ package
        public int OrderTransactionCount { get; set; } // Số giao dịch mua hàng
        public decimal OrderRevenue { get; set; } // Doanh thu từ order
        public decimal TotalRevenue { get; set; } // Tổng doanh thu (bao gồm 3% phí trên mỗi đơn hàng)
    }
}
