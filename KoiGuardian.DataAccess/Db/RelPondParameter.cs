using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class RelPondParameter
    {
        public int PondId { get; set; }

        public int ParameterUnitID { get; set; }

        public DateTime CalculatedDate { get; set; }

        public float Value { get; set; }

        public Pond? Pond { get; set; } 

        public ParameterUnit? ParameterUnit { get; set; }
    }
}
