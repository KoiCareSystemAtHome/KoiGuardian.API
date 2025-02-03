using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using System;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Services
{
    public interface ISaltCalculatorService
    {
        Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request);
    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly Dictionary<string, double> StandardSaltPercentDict = new Dictionary<string, double>
{
    { "Low", 0.3 },      
    { "Medium", 0.5 },   
    { "High", 0.7 }      
};

        public async Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request)
        {
            // Lấy StandardSaltPercent từ dictionary, nếu không có thì mặc định là 0
            StandardSaltPercentDict.TryGetValue(request.StandardSaltLevel, out double standardSalt);

            double requiredSaltPercent = standardSalt + request.SaltModifyPercent;
            double totalSalt = request.WaterWeight * (requiredSaltPercent / 100);

            if (totalSalt < request.LowerBound || totalSalt > request.UpperBound)
            {
                throw new ArgumentOutOfRangeException("Lượng muối tính được nằm ngoài giới hạn cho phép.");
            }

            return await Task.FromResult(new CalculateSaltResponse
            {
                PondId = request.PondId,
                TotalSalt = totalSalt
            });
        }

    }
}
