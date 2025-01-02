using System.ComponentModel.DataAnnotations;

namespace KoiGuardian.DataAccess.Db
{
    public class Shop
    {
        
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
        public decimal ShopRate { get; set; }
        public string ShopDescription { get; set; }
        public string ShopAddress { get; set; }
        public bool IsActivate { get; set; }
        public string BizLicences { get; set; }

        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        
        public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    }
}
