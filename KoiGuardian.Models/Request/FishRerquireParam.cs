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
        public Guid ParameterID { get; set; }
        public double? WeightWarningUpper { get; set; }
        public double? WeightWarningLowwer { get; set; }
        public double? WeightDangerLower { get; set; }
        public double? WeightDangerUpper { get; set; }


        public double? SizeWarningUpper { get; set; }
        public double? SizeWarningLowwer { get; set; }
        public double? SizeDangerLower { get; set; }
        public double? SizeDangerUpper { get; set; }
        public string MeasurementInstruction { get; set; }
    }
}
