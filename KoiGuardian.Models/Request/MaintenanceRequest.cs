using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class MaintenanceRequest
    {
        public Guid PondId { get; set; }
        public double CurrentNitrogenDensity { get; set; }
        public double AverageNitrogenIncreasePerDay { get; set; }
    }
}
