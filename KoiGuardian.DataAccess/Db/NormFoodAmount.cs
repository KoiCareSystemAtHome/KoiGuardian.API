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
        public DesiredGrouthType DesiredGrouthType {  get; set; }   
        public string WaterTemperatureID {  get; set; }
        public float StandardAmount {  get; set; } // percent food compare to fish weight
        public int FeedingOften {  get; set; }
        public int AgeFrom { get; set; } // từ số tháng tuổi 
        public int AgeTo { get; set; } // tới số tháng tuồi

    }

    public enum DesiredGrouthType
    {
        low,
        medium,
        high
    }
}
