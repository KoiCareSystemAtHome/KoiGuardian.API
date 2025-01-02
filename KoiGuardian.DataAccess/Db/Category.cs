using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;

public class Category
{
    public Guid CategoryId { get; set; }
    public Guid ShopId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    // Navigation properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual Shop Shop { get; set; } = null!;
}
