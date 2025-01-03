namespace KoiGuardian.Models.Request
{
    public class PondRerquireParam
    {
        public Guid ParameterUntiID { get; set; }
        public string ParameterName { get; set; }
        public string UnitName { get; set; }
        public double? WarningUpper { get; set; }
        public double? WarningLowwer { get; set; }
        public double? DangerLower { get; set; }
        public double? DangerUpper { get; set; }
        public string MeasurementInstruction { get; set; }
    }
}
