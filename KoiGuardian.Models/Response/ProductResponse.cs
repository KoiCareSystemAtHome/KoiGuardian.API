using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class ProductResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class ProductDetailResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Brand { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ParameterImpactment { get; set; }
        public CategoryInfo Category { get; set; }
        public ShopInfo Shop { get; set; }
        public List<FeedbackInfo> Feedbacks { get; set; }
    }

    public class CategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
    }

    public class ShopInfo
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
    }

    public class FeedbackInfo
    {
        public Guid FeedbackId { get; set; }
        public string MemberName { get; set; }
        public int Rate { get; set; }
        public string Content { get; set; }
    }
}
