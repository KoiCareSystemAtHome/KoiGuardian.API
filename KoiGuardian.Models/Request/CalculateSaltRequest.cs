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
            public double WaterWeight { get; set; } // Khối lượng nước trong hồ
            public string StandardSaltLevel { get; set; }
            public double SaltModifyPercent { get; set; } // % muối thêm khi cá ốm
            public double LowerBound { get; set; } // Giới hạn dưới của lượng muối
            public double UpperBound { get; set; } // Giới hạn trên của lượng muối
        }
    
}
