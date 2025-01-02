namespace KoiGuardian.DataAccess.Db;

public class Feedback
{
    public Guid FeedbackId { get; set; }
    public Guid OrderDetailId { get; set; }    
    public Guid MemberId { get; set; }
    public int Rate { get; set; }
    public string Content { get; set; }

    public virtual User? Member { get; set; }
    public virtual OrderDetail? OrderDetail { get; set; }    

}
