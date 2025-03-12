using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class NotificationRequest
    {
        public Guid PondId { get; set; }
        public DateTime StartTime { get; set; }
        public int HoursInterval { get; set; } = 7; // Mặc định là 7 giờ
        
    }
    public class UpdateWaterReplacementTimeRequest
    {
        public Guid PondId { get; set; }
        public DateTime NewReductionStartTime { get; set; }
    }

}
