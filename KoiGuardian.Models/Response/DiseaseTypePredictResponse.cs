namespace KoiGuardian.Models.Response;

public class DiseaseTypePredictResponse
{
    public List<SymptomPredict> SymptomPredicts { get; set; }   
    public string CauseGroupType { get; set; }
}


public class SymptomPredict
{
    public Guid SymtompId { get; set; }
    public string Name { get; set; }
    public bool SymptomPriority { get; set; }
    public string SymptomUnit { get; set; }
}