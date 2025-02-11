using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class NormFoodAmount
    {
        public Guid NormFoodAmountId {  get; set; }   
        public string WaterTemperatureID {  get; set; }
        public float StandardAmount {  get; set; } // percent food compare to fish weight
        public string  FeedingFrequency {  get; set; }
        public int AgeFrom { get; set; } // từ số tháng tuổi 
        public int AgeTo { get; set; } // tới số tháng tuồi
        public string WarningMessage { get; set; } = string.Empty;
        public float TemperatureUpper { get; set; } // temperature in standard unit : Cencius
        public float TemperatureLower { get; set; } // temperature in standard unit : Cencius
    }

    public enum DesiredGrouthType
    {
        low,
        medium,
        high
    }
}
