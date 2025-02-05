using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface ISaltCalculatorService
    {
        Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request);
    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;

        private readonly Dictionary<string, double> _standardSaltPercentDict = new()
        {
            { "Low", 0.03 },
            { "Medium", 0.05 },
            { "High", 0.07 }
        };

        public SaltCalculatorService(
            IRepository<Pond> pondRepository,
            IRepository<RelPondParameter> pondParamRepository)
        {
            _pondRepository = pondRepository;
            _pondParamRepository = pondParamRepository;
        }

        public async Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request)
        {
            // Kiểm tra đầu vào
            if (request.PondId == Guid.Empty)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Invalid PondId provided." }
                };
            }

            // Lấy thông tin hồ từ cơ sở dữ liệu
            var pond = await _pondRepository.GetAsync(
                u => u.PondID == request.PondId,
                include: u => u.Include(p => p.Fish));

            // Debug: Explicit check for pond and MaxVolume
            if (pond == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> {
                        "Pond not found.",
                        $"Checked PondId: {request.PondId}"
                    }
                };
            }

            // Debug: Explicit logging of MaxVolume
            if (pond.MaxVolume <= 0)
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> {
            $"Invalid Pond Volume: {pond.MaxVolume}",
            "Kiểm tra lại giá trị MaxVolume trong cơ sở dữ liệu"
        }
                };
            }

            // Kiểm tra cá trong hồ
            if (pond.Fish == null || !pond.Fish.Any())
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "No fish found in this pond." }
                };
            }

            // Kiểm tra giá trị StandardSaltLevel
            if (!_standardSaltPercentDict.TryGetValue(request.StandardSaltLevel, out double standardSalt))
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> {
                        $"Invalid StandardSaltLevel: {request.StandardSaltLevel}",
                        "Accepted values: Low, Medium, High"
                    }
                };
            }


            var saltParameter = await _pondParamRepository.GetAsync(
     u => u.PondId == pond.PondID && u.Parameter.Name.ToLower() == "salt",  
     include: u => u.Include(p => p.Parameter)
 );

            if (saltParameter?.Parameter == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> {
            "Salt parameter not found for this pond.",
            "Ensure salt parameter is configured in database"
        }
                };
            }


            var parameter = saltParameter.Parameter;

            // Tính tổng lượng muối yêu cầu
            double pondVolume = pond.MaxVolume; // Volume in liters
            double requiredSaltPercent = standardSalt + 0.01; // Standard salt percentage
            double totalSaltWeight = pondVolume * (requiredSaltPercent / 100) * 1000; // Total salt weight in mg
            double saltConcentrationMgPerL = totalSaltWeight / pondVolume; 

            // Kiểm tra theo các giới hạn trong Parameter
            var notes = new List<string>();

            if (saltConcentrationMgPerL < (parameter.WarningLowwer ?? 0))
            {
                notes.Add("Lượng muối dưới mức cảnh báo.");
            }
            else if (saltConcentrationMgPerL > (parameter.WarningUpper ?? double.MaxValue))
            {
                notes.Add("Lượng muối vượt mức cảnh báo.");
            }

            if (saltConcentrationMgPerL < (parameter.DangerLower ?? 0))
            {
                notes.Add("Lượng muối dưới mức nguy hiểm. Cá có thể gặp nguy hiểm.");
            }
            else if (saltConcentrationMgPerL > (parameter.DangerUpper ?? double.MaxValue))
            {
                notes.Add("Lượng muối vượt mức nguy hiểm. Cá có thể gặp nguy hiểm.");
            }

            // Trả về kết quả
            return new CalculateSaltResponse
            {
                PondId = pond.PondID,
                TotalSalt = saltConcentrationMgPerL,
                AdditionalInstruction = notes
            };
        }
    }
}