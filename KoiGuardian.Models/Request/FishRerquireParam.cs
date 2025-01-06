using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class FishRerquireParam
    {
        public Guid ParameterID { get; set; }
        public string ParameterName { get; set; }
        public List<FishRerquireParamUnit>? ParameterUnits { get; set; }
    }

    public class FishRerquireParamUnit
    {
        public Guid ParameterUntiID { get; set; }
        public string UnitName { get; set; }
        public double? WarningUpper { get; set; }
        public double? WarningLowwer { get; set; }
        public double? DangerLower { get; set; }
        public double? DangerUpper { get; set; }
        public string MeasurementInstruction { get; set; }
    }

}
