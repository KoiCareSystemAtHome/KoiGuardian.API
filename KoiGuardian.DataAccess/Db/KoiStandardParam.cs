using System.ComponentModel.DataAnnotations;

namespace KoiGuardian.DataAccess.Db;

public class KoiStandardParam
{
    [Key]
    public Guid HistoryId {  get; set; }
    public Guid ParameterID {  get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Type { get; set; }

    public Guid? VarietyId { get; set; }
    public string UnitName { get; set; }
    public double? WarningUpper { get; set; }
    public double? WarningLowwer { get; set; }
    public double? DangerLower { get; set; }
    public double? DangerUpper { get; set; }
    public bool IsActive { get; set; }
    public string MeasurementInstruction { get; set; }
    public int AgeFrom { get; set; } // từ số tháng tuổi 
    public int AgeTo { get; set; } // tới số tháng tuồi
    public int WarningAcceptantDay { get; set; } = 5;// số ngày cá có thể sống trong hồ nếu param vượt mức warning
    public int DangerAcceptantDay { get; set; } = 3;// số ngày cá có thể sống trong hồ nếu param vượt mức danger
    public DateTime? ValidUntil { get; set; }


}

public enum ParameterType
{
    Fish,
    Pond, 
    Disease,

};

