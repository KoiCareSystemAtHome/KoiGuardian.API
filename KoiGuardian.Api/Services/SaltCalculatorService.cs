using KoiGuardian.Models.Request;

namespace KoiGuardian.Api.Services
{
    public interface ISaltCalculatorService
    {
        double CalculateSalt(CalculateSaltRequest request);
    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        public double CalculateSalt(CalculateSaltRequest request)
        {
            
            double requiredSaltPercent = request.StandardSaltPercent + request.SaltModifyPercent;

            
            double totalSalt = request.WaterWeight * (requiredSaltPercent / 100);

            
            if (totalSalt < request.LowerBound || totalSalt > request.UpperBound)
            {
                throw new ArgumentOutOfRangeException("Lượng muối tính được nằm ngoài giới hạn cho phép.");
            }

            return totalSalt;
        }
    }
}
