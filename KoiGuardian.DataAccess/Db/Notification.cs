using Microsoft.Identity.Client;

namespace KoiGuardian.DataAccess.Db
{
    public class Notification
    {
        public Guid NotificationId { get; set; } 
        public Guid ReceiverId { get; set; } 
        public string Type { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime Seendate { get; set; }
        public string Data { get; set; } // json deseriallize 
    }
}
