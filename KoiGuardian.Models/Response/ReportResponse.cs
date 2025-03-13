using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class ReportResponse
    {
        public string message { get; set; }
        public string status { get; set; }
    }

    public class ReportDetailResponse
    {
        public Guid ReportId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Reason { get; set; }
        public string image { get; set; }
        public string status { get; set; }
        public OrderDetailResponse order { get; set; }
    }
}
