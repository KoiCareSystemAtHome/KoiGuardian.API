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
        public string StandardSaltLevel { get; set; }
        public double WaterChangePercent { get; set; } // Giá trị phần trăm lượng nước thay đổi
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
        public double TargetSaltLevel { get; set; }  
    }



}
