using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Report
    {
        [Key]
        public Guid ReportId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Reason { get; set; }
        public string image { get; set; }
        public string status { get; set; }

        public virtual Order Order { get; set; }
    }
}
