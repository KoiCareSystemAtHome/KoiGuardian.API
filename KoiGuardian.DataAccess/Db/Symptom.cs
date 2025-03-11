using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class Symptom
{
    [Key]

    public Guid SymtompId { get; set; }
    public string Name { get; set; }
    public bool SymptomPriority { get; set; }
    public string SymptomUnit { get; set; }
    public string Type { get; set; }
}

public enum SymptomType
{
    Common,
    Common_Food,
    Common_Enviroment,
    Common_Disease
}

public enum SymptomUnit
{
    check,
    rate, // from 1 to 5 
    range
}
