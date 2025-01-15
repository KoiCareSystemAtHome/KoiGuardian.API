using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class 
        Mode
    {
        [Key]
        public Guid ModeId { get; set; } 
        public Guid PondId { get; set; }
        public string ModeName { get; set; } = string.Empty;
        public int Period { get; set; } // max day that the pond in period  
        public string WarningMessage { get; set; } = string.Empty;
        public float TemperatureUpper { get; set; } // temperature in standard unit : Cencius
        public float TemperatureLower { get; set; } // temperature in standard unit : Cencius
        public int AgeFrom { get; set; } // từ số tháng tuổi 
        public int AgeTo { get; set; } // tới số tháng tuồi
    }
}
