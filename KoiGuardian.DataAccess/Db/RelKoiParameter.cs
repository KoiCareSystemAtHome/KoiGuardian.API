using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class RelKoiParameter
{
    [Key]
    public Guid RelKoiParameterID {  get; set; }
    public Guid KoiId {  get; set; }

    public Guid ParameterUnitID { get; set; }

    public DateTime CalculatedDate { get; set; }

    public float Value { get; set; }

    public Fish? Fish { get; set; }

    public virtual ParameterUnit? ParameterUnit { get; set; }
}
