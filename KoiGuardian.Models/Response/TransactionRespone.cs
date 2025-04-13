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
        public Guid DocNo { get; set; }
        public string TransactionType { get; set; }
        public string VnPayTransactionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentInfo Payment { get; set; }
        public RefundInfo Refund { get; set; }
    }

    public class RevenueSummaryDto
    {
        public int PackageTransactionCount { get; set; } // Số giao dịch mua package
        public decimal PackageRevenue { get; set; } // Doanh thu từ package
        public int OrderTransactionCount { get; set; } // Số giao dịch mua hàng
        public decimal OrderRevenue { get; set; } // Doanh thu từ order
        public decimal TotalRevenue { get; set; } // Tổng doanh thu (bao gồm 3% phí trên mỗi đơn hàng)
    }

    public class TransactionPackageDto
    {
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public string PakageName { get; set; }

        public DateTime ExpiryDate { get; set; }
        public decimal Amount { get; set; }
    }


    public class OrderStatusSummaryDto
    {
        public int SuccessfulOrders { get; set; }
        public int FailedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalOrders { get; set; }
    }

    public class ProductSalesSummaryDto
    {
        public List<ProductSalesByMonthDto> MonthlySales { get; set; } = new();
        public int TotalFood { get; set; }
        public int TotalProducts { get; set; }
        public int TotalMedicines { get; set; }
        public int TotalItemsSold { get; set; }
    }

    public class ProductSalesByMonthDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int FoodCount { get; set; }
        public int ProductCount { get; set; }
        public int MedicineCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class PaymentInfo
    {
        public decimal Amount { get; set; }  // Sử dụng decimal thay cho float để chính xác hơn với tiền tệ
        public DateTime Date { get; set; }
        public string PaymentMethod { get; set; } 
        public string Description { get; set; }
    }

    public class RefundInfo
    {
        public decimal Amount { get; set; }  // Sử dụng decimal thay cho float để chính xác hơn với tiền tệ
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }

}
