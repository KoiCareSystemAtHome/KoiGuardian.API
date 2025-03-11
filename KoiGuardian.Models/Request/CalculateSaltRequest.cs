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

    public class AdjustSaltStartTimeRequest
    {
        public Guid PondId { get; set; }
        public DateTime NewStartTime { get; set; }
    }



}
