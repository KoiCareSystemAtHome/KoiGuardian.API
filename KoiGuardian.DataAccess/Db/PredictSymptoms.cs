using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class PredictSymptoms
{
    [Key]

    public Guid SymtompId { get; set; }
    public string Name { get; set; }
    public bool SymptomPriority { get; set; }
    public string SymptomUnit { get; set; }
    public string Type { get; set; }

    public virtual IEnumerable<RelPredictSymptomDisease> RelPredictSymptomDiseases {  get; set; }
}