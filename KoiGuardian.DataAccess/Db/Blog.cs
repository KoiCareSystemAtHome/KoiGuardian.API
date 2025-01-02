using KoiGuardian.DataAccess.Db;

public class Blog
{
    public Guid BlogId { get; set; }
    public bool IsApproved { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Images { get; set; }
    public string Tag { get; set; }
    public int View { get; set; }
    public Guid ShopId { get; set; }
    public DateTime? ReportedDate { get; set; }
    public string ReportedBy { get; set; }

    public virtual Shop Shop { get; set; }
    public virtual ICollection<BlogProduct> BlogProducts { get; set; } = new List<BlogProduct>();
}
