using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CalculateFoodRequest
    {
        public Guid PondId { get; set; }    
        public string DesiredGrowth { get; set; }
        public int TemperatureLower { get; set; }
        public int TemperatureUpper { get; set; }
    }


}
