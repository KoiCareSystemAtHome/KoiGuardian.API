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
    }

    public class RecurringMaintenance
    {
        public Guid PondId { get; set; }
        public DateTime endDate { get; set; }
        public int cycleDays { get; set; }
    }
}

