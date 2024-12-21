namespace KoiGuardian.Models.Commons;

public class BaseEntity()
{
    public required string CreatedBy { get; set; }   = string.Empty;

    public DateTime CreatedDate { get; set; }   = DateTime.Now;

    public string UpdatedBy { get; set; }   = string.Empty;

    public DateTime UpdatedDate { get; set; }   = DateTime.Now;
}