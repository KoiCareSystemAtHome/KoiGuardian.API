using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CalculateSaltRequest
    {
        public Guid PondId { get; set; }
        public string StandardSaltLevel { get; set; } // "Low", "Medium", "High"
        public double WaterChangePercent { get; set; }
       /* public double AddedSalt { get; set; } // Lượng muối đã thêm (kg)*/
    }

    public class SaltAdditionRecord
    {
        public Guid PondId { get; set; }
        public double SaltAmount { get; set; }
        public DateTime AddedTime { get; set; }
    }

    public class AddSaltRequest
    {
        public Guid PondId { get; set; }
        public double TargetSaltWeightKg { get; set; }
    }

    public class SaltReminderRequest
    {
        public Guid PondId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime MaintainDate { get; set; }
    }

    public class GenerateSaltRemindersRequest
    {
        public Guid PondId { get; set; }
      
        public int CycleHours { get; set; }   
    }

    public class SaveSaltRemindersRequest
    {
        public Guid PondId { get; set; }
        public List<SaltReminderRequest> Reminders { get; set; } = new List<SaltReminderRequest>();
    }

    public class UpdateSaltReminderRequest
    {
        public Guid PondReminderId { get; set; }
        public DateTime NewMaintainDate { get; set; }
    }

    // Existing request model for adjusting start time
    public class AdjustSaltStartTimeRequest
    {
        public Guid PondId { get; set; }
        public DateTime NewStartTime { get; set; }
    }

    // Existing request model for updating salt amount
    public class UpdateSaltAmountRequest
    {
        public Guid PondId { get; set; }
        public double AddedSaltKg { get; set; }
    }


}
