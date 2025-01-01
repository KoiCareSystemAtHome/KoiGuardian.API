using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class Symptom
{
    public Guid SymtompId { get; set; }
    public string Name { get; set; }
    public string NormalRange { get; set; }
    public SymptomType Type { get; set; }
}

public enum SymptomType
{
    Common,
    Food,
    Enviroment,
    Disease
}
