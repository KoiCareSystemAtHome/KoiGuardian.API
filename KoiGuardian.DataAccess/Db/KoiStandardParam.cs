using System.ComponentModel.DataAnnotations;

namespace KoiGuardian.DataAccess.Db;

public class KoiStandardParam
{
    [Key]
    public Guid ParameterID {  get; set; }


    public DateTime CreatedAt { get; set; }

    public double? WeightWarningUpper { get; set; }
    public double? WeightWarningLowwer { get; set; }
    public double? WeightDangerLower { get; set; }
    public double? WeightDangerUpper { get; set; }


    public double? SizeWarningUpper { get; set; }
    public double? SizeWarningLowwer { get; set; }
    public double? SizeDangerLower { get; set; }
    public double? SizeDangerUpper { get; set; }
    public bool IsActive { get; set; }
    public string MeasurementInstruction { get; set; }
    public int Age { get; set; } // tới số tháng tuồi
}

public enum ParameterType
{
    Fish,
    Pond, 
    Disease,

};

