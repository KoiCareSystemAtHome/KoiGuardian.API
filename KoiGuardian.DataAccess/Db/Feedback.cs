namespace KoiGuardian.DataAccess.Db;

public class Feedback
{
    public Guid FeedbackId { get; set; }
    public Guid ProductId { get; set; }    
    public string MemberId { get; set; }
    public int Rate { get; set; }
    public string Content { get; set; } = string.Empty;

    public virtual User? Member { get; set; }
    public virtual Product? Product { get; set; }     
}
