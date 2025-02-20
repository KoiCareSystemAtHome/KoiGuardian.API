using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class RelPredictSymptomDisease
{
    [Key]
    public Guid RelSymptomDiseaseId { get; set; }
    public Guid DiseaseId { get; set; }
    public Guid SymtompId { get; set; }
    public float DiseaseUpper { get; set; }
    public float DiseaseLower { get; set; }

    public virtual PredictSymptoms? PredictSymptoms { get; set; }
    public virtual Disease? Disease { get; set; }
}

