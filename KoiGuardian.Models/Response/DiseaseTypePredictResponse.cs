namespace KoiGuardian.Models.Response;

public class DiseaseTypePredictResponse
{
    public List<SymptomPredict> SymptomPredicts { get; set; }   
    public string CauseGroupType { get; set; }
}

public class FinalDiseaseTypePredictResponse
{
    public string CauseGroupType { get; set; }
    public Guid DiseaseId { get; set; }

    public string DiseaseName { get; set; }
    public string Description { get; set; }

}


public class SymptomPredict
{
    public Guid SymtompId { get; set; }
    public string Name { get; set; }
    public bool SymptomPriority { get; set; }
    public string SymptomUnit { get; set; }
}

public class MedicinePredict
{
    public Guid MedicineId { get; set; }
    public string Instruction { get; set; }
    public string Data { get; set; } // List ProductID in serialization
}