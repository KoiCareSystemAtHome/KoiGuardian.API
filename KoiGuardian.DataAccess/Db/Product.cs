using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoiGuardian.DataAccess.Db
{
    public class Product
    {
       
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; }
        public string Brand { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }

       
        public string ShopId { get; set; }
      
        public virtual Shop Shop { get; set; }

        
        public virtual ICollection<BlogProduct> BlogProducts { get; set; } = new List<BlogProduct>();
    }
}
