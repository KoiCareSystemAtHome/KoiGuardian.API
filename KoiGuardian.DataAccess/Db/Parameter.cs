using System.ComponentModel.DataAnnotations;

namespace KoiGuardian.DataAccess.Db;

public class Parameter
{
    [Key]
    public Guid ParameterID {  get; set; }

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

