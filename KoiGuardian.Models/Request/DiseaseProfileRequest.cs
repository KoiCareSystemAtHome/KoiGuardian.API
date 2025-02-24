namespace KoiGuardian.Models.Request;

public class DiseaseProfileRequest
{
    public Guid DiseaseID { get; set; }
    public Guid MedicineId { get; set; }
    public Guid FishId { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; }
    public List<SymptomsInput> Symptoms { get; set; } // jsonb deserialization 
    public string Note { get; set; }
}

public class SymptomsInput
{
    public Guid SymptomID { get; set; }
    public string Value { get; set; }
}

public class UpdateDiseaseProfileRequest
{
    public Guid? DiseaseID { get; set; }
    public string Status { get; set; }
    public Guid? MedicineId { get; set; }
    public string? Note { get; set; }
    public List<SymptomsInput> Symptoms { get; set; }
}

