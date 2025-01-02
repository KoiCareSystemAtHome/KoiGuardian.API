namespace KoiGuardian.DataAccess.Db;

public class RelSymptomDisease
{
    public Guid RelSymptomDiseaseId { get; set; }
    public Guid DiseaseId { get; set; }
    public Guid SymtompId { get; set; }
    public Guid ParameterUnitId { get; set; }
    public float DiseaseUpper { get; set; }
    public float DiseaseLower { get; set; }

    public virtual Symptom? Symptom { get; set; }   
    public virtual Disease? Disease { get; set; }   
    public virtual ParameterUnit? ParameterUnit { get; set; }
}

