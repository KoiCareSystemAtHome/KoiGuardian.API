using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CreateReportRequest
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; }
        public string Image { get; set; }
    }

    public class UpdateReportRequest
    {
        public Guid ReportId { get; set; } 
        public string statuz  { get; set; }
    }
}
