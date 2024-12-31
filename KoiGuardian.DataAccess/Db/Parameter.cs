namespace KoiGuardian.DataAccess.Db;

public class Parameter
{
    public int ParameterID {  get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public ParameterType Type { get; set; }

    public IEnumerable<ParameterUnit>? ParameterUnits { get; set; }
}

public enum ParameterType
{
    Fish,
    Pond, 
    Disease
};

