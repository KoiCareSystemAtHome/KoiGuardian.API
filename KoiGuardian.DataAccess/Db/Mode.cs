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
        public int Period { get; set; } // number of days in prediod 
        public string WarningMessage { get; set; } = string.Empty;
        public float TemperatureUpper { get; set; } // temperature in standard unit : Cencius
        public float TemperatureLower { get; set; } // temperature in standard unit : Cencius


        public virtual Pond? Pond { get; set; }
    }
}
