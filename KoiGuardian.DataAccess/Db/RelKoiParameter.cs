using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class RelKoiParameter
{
    public int KoiId {  get; set; }

    public int ParameterUnitID { get; set; }

    public DateTime CalculatedDate { get; set; }

    public float Value { get; set; }

    public Fish? Fish { get; set; }

    public ParameterUnit? ParameterUnit { get; set; }
}
