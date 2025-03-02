namespace KoiGuardian.Models.Request
{
    public class PondRerquireParam
    {
        public Guid ParameterId { get; set; }
        public string ParameterName { get; set; }
        public string UnitName { get; set; }
        public double? WarningUpper { get; set; }
        public double? WarningLowwer { get; set; }
        public double? DangerLower { get; set; }
        public double? DangerUpper { get; set; }
        public string MeasurementInstruction { get; set; }
    }

    public class PondParameterInfo
    {
        public Guid ParameterUnitID { get; set; }
        public string UnitName { get; set; }
        public double? WarningLowwer { get; set; }
        public double? WarningUpper { get; set; }
        public double? DangerLower { get; set; }
        public double? DangerUpper { get; set; }
        public string MeasurementInstruction { get; set; }
        public List<ValueInfor> valueInfors { get; set; }
    }

    public class ValueInfor
    {
        public DateTime caculateDay { get; set; }
        public double Value { get; set; }
    }
}
