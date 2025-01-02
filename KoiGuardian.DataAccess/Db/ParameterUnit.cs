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
    public Guid HistoryID {  get; set; }
    
    public DateTime? ValidUnitl {  get; set; }

    public Guid ParameterUnitID { get; set; }
    public Guid ParameterID { get; set; }
    public Guid VarietyId { get; set; }
    public string UnitName { get; set; }
    public float? WarningUpper { get; set; }
    public float? WarningLowwer { get; set; }
    public float? DangerLower { get; set; }
    public float? DangerUpper { get; set; }
    public bool IsStandard { get; set; }
    public bool IsActive { get; set; }
    public float Convertionrate { get; set; }
    public string MeasurementInstruction { get; set; }
    public int AgeFrom { get; set; } // từ số tháng tuổi 
    public int AgeTo { get; set; } // tới số tháng tuồi

    public Parameter? Parameter { get; set; }

}
