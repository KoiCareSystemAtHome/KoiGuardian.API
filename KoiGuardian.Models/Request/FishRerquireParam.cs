using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class FishRerquireParam
    {
        [Key]
        public Guid HistoryID { get; set; }
        public Guid ParameterID { get; set; }
        public string ParameterName { get; set; }
        public string UnitName { get; set; }
        public double? WarningUpper { get; set; }
        public double? WarningLowwer { get; set; }
        public double? DangerLower { get; set; }
        public double? DangerUpper { get; set; }
        public string MeasurementInstruction { get; set; }
    }
}
