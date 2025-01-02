using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class ParameterUnit
{
    [Key]
    public int HistoryID {  get; set; }
    
    public DateTime? ValidUnitl {  get; set; }

    public int ParameterUnitID { get; set; }
    public int ParameterID { get; set; }
    public string UnitName { get; set; }
    public float WarningUpper { get; set; }
    public float WarningLowwer { get; set; }
    public float DangerLower { get; set; }
    public float DangerUpper { get; set; }
    public bool IsStandard { get; set; }
    public bool IsActive { get; set; }
    public float Convertionrate { get; set; }
    public string MeasurementInstruction { get; set; }

    public Parameter? Parameter { get; set; }

}
