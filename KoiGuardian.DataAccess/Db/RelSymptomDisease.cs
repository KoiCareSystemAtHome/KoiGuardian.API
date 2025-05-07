using System.ComponentModel.DataAnnotations;

namespace KoiGuardian.DataAccess.Db;

public class RelSymptomDisease
{
    [Key]
    public Guid RelSymptomDiseaseId { get; set; }
    public Guid DiseaseId { get; set; }
    public Guid SymptomSymtompId { get; set; }
    public Guid SymtompId { get; set; }
    public float DiseaseUpper { get; set; }
    public float DiseaseLower { get; set; }

    public virtual Symptom? Symptom { get; set; }   
    public virtual Disease? Disease { get; set; }   
}

