using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface ISaltCalculatorService
    {
        Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request);
        Task<SaltAdditionProcessResponse> GetSaltAdditionProcess(Guid pondId);


    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;
        private readonly IRepository<PondStandardParam> _pondStandardParamRepository;
        private readonly IRepository<KoiDiseaseProfile> _koiDiseaseProfileRepository;
        private static readonly ConcurrentDictionary<Guid, CalculateSaltResponse> _saltCalculationCache = new();





        public SaltCalculatorService(
            IRepository<Pond> pondRepository,
            IRepository<KoiDiseaseProfile> koiDiseaseProfile,
            IRepository<RelPondParameter> pondParamRepository)
            
        {
            _pondRepository = pondRepository;
            _pondParamRepository = pondParamRepository;
            _koiDiseaseProfileRepository = koiDiseaseProfile;
        }
       

        private readonly Dictionary<string, double> _standardSaltPercentDict = new()
{
            { "low", 0.003 },
            { "medium", 0.005 },
            { "high", 0.007 }
        };

        public async Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request)
        {
            // Fetch pond with fish included
            var pondQuery = _pondRepository.GetQueryable(p => p.PondID == request.PondId)
                .Include(p => p.Fish); // Apply Include directly on IQueryable
            var pond = await pondQuery.FirstOrDefaultAsync();

            if (pond == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Pond not found." }
                };
            }

            // Fetch salt parameter
            var saltParamQuery = _pondParamRepository.GetQueryable(p => p.Parameter.Name.ToLower() == "salt")
                .Include(p => p.Parameter); // Apply Include directly on IQueryable
            var saltParameter = await saltParamQuery.FirstOrDefaultAsync();

            if (saltParameter?.Parameter == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Salt parameter not found in standard parameters." }
                };
            }

            // Get current salt value for the pond
            var currentSaltQuery = _pondParamRepository.GetQueryable(
                p => p.PondId == request.PondId && p.Parameter.ParameterID == saltParameter.Parameter.ParameterID)
                .Include(p => p.Parameter); // Apply Include directly on IQueryable
            var currentSaltValue = await currentSaltQuery.FirstOrDefaultAsync();

            double currentVolume = request.WaterChangePercent > 0
                ? pond.MaxVolume * (1 - request.WaterChangePercent / 100)
                : pond.MaxVolume;

            if (request.WaterChangePercent == 100)
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    CurrentSalt = 0,
                    SaltNeeded = 0,
                    WaterNeeded = 0,
                    AdditionalInstruction = new List<string> { "Hồ không có nước, không thể thêm muối." }
                };
            }
            else if (request.WaterChangePercent > 100)
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    CurrentSalt = 0,
                    SaltNeeded = 0,
                    WaterNeeded = 0,
                    AdditionalInstruction = new List<string> { "Mực nước hiện tại không hợp lí vui lòng kiểm tra lại" }
                };
            }

            double currentSaltConcentration = currentSaltValue?.Value ?? 0;

            /*if (request.AddedSalt > 0)
            {
                currentSaltConcentration += request.AddedSalt;
            }*/

            if (!_standardSaltPercentDict.TryGetValue(request.StandardSaltLevel.ToLower(), out double standardSalt))
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Invalid StandardSaltLevel. Accepted values: Low, Medium, High" }
                };
            }

            var additionalNotes = new List<string>();

            if (request.StandardSaltLevel.ToLower() == "high")
            {
                additionalNotes.Add("Nếu có cá bệnh truyền nhiễm, nên tách hồ để tránh ảnh hưởng đến các con cá khác.");
            }

            // Check fish diseases and adjust salt percentage
            double saltModifyPercent = 0;

            // Ensure Fish collection is not null
            var fishList = pond.Fish?.ToList() ?? new List<Fish>();
            if (fishList.Any())
            {
                foreach (var fish in fishList)
                {
                    // Fetch disease profiles for the current fish
                    var diseaseProfilesQuery = _koiDiseaseProfileRepository.GetQueryable(d => d.FishId == fish.KoiID)
                        .Include(d => d.Disease); // Apply Include directly on IQueryable
                    var diseaseProfiles = await diseaseProfilesQuery.ToListAsync();

                    foreach (var diseaseProfile in diseaseProfiles)
                    {
                        if (diseaseProfile.Status == ProfileStatus.Accept || diseaseProfile.Status == ProfileStatus.Pending)
                        {
                            if (diseaseProfile.Disease != null)
                            {
                                saltModifyPercent += diseaseProfile.Disease.SaltModifyPercent;
                                additionalNotes.Add($"Fish {fish.Name} has disease '{diseaseProfile.Disease.Name}' affecting salt by {diseaseProfile.Disease.SaltModifyPercent}%.");
                            }
                        }
                    }
                }
            }
            else
            {
                additionalNotes.Add("No fish found in the pond.");
            }

            // Calculate required salt with disease adjustment
            double requiredSaltPercent = standardSalt + saltModifyPercent;
            double targetSaltWeightKg = currentVolume * requiredSaltPercent;
            double additionalSaltNeeded = targetSaltWeightKg - currentSaltConcentration;
            double saltConcentrationMgPerL = (currentSaltConcentration * 1000) / currentVolume;

            if (additionalSaltNeeded < 0)
            {
                additionalNotes.Add($"Current salt ({currentSaltConcentration:F2} kg) exceeds target ({targetSaltWeightKg:F2} kg).");

                double currentSaltConcentrationPercent = currentSaltConcentration / currentVolume * 100;
                double newTotalVolume = (currentVolume * currentSaltConcentrationPercent) / requiredSaltPercent;
                double additionalWaterNeeded = newTotalVolume - currentVolume;

                additionalNotes.Add($"Need to add {additionalWaterNeeded:F2} l water to reduce salt concentration to target level.");

                if (newTotalVolume > pond.MaxVolume)
                {
                    double excessVolume = newTotalVolume - pond.MaxVolume;
                    additionalNotes.Add($"Warning: Adding {additionalWaterNeeded:F2} l water exceeds pond capacity by {excessVolume:F2} l.");
                }

                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = targetSaltWeightKg,
                    CurrentSalt = currentSaltConcentration,
                    SaltNeeded = additionalSaltNeeded,
                    WaterNeeded = additionalWaterNeeded,
                    AdditionalInstruction = additionalNotes
                };
            }
            else if (additionalSaltNeeded > 0)
            {
                additionalNotes.Add($"Need to add: {additionalSaltNeeded:F2} kg salt.");
                additionalNotes.Add($"Current salt: {currentSaltConcentration:F2} kg.");
                additionalNotes.Add($"Target salt: {targetSaltWeightKg:F2} kg.");
            }

            // Add warnings based on salt concentration thresholds
            if (saltConcentrationMgPerL < saltParameter.Parameter.WarningLowwer)
                additionalNotes.Add("Salt level below warning threshold.");
            if (saltConcentrationMgPerL > saltParameter.Parameter.WarningUpper)
                additionalNotes.Add("Salt level above warning threshold.");

            if (saltConcentrationMgPerL < saltParameter.Parameter.DangerLower)
                additionalNotes.Add("Salt level below danger threshold. Fish may be at risk.");
            if (saltConcentrationMgPerL > (saltParameter.Parameter.DangerUpper ?? double.MaxValue))
                additionalNotes.Add("Salt level above danger threshold. Fish may be at risk.");

            var response = new CalculateSaltResponse
            {
                PondId = request.PondId,
                TotalSalt = targetSaltWeightKg,
                CurrentSalt = currentSaltConcentration,
                SaltNeeded = additionalSaltNeeded,
                AdditionalInstruction = additionalNotes
            };

            // Lưu kết quả vào cache
            _saltCalculationCache[request.PondId] = response;
            return response;
        }

        public async Task<SaltAdditionProcessResponse> GetSaltAdditionProcess(Guid pondId)
        {
            // Kiểm tra xem có kết quả CalculateSalt nào cho PondId này không
            if (!_saltCalculationCache.TryGetValue(pondId, out CalculateSaltResponse saltResponse))
            {
                return new SaltAdditionProcessResponse
                {
                    PondId = pondId,
                    Instructions = new List<string> { "No salt calculation found for this pond. Please calculate salt first." }
                };
            }

            var pondQuery = _pondRepository.GetQueryable(p => p.PondID == pondId)
                .Include(p => p.Fish);
            var pond = await pondQuery.FirstOrDefaultAsync();

            if (pond == null)
            {
                return new SaltAdditionProcessResponse
                {
                    PondId = pondId,
                    Instructions = new List<string> { "Pond not found." }
                };
            }

            double additionalSaltNeeded = saltResponse.SaltNeeded;

            var instructions = new List<string>();

            if (additionalSaltNeeded <= 0)
            {
                instructions.Add("No additional salt needed or current salt exceeds target.");
                return new SaltAdditionProcessResponse
                {
                    PondId = pondId,
                    Instructions = instructions
                };
            }

            // Quy tắc 1: Tăng tối đa 0.05% mỗi lần
            double maxSaltIncreasePerTime = pond.MaxVolume * 0.0005; // 0.05% của thể tích tối đa
            int numberOfAdditions = (int)Math.Ceiling(additionalSaltNeeded / maxSaltIncreasePerTime);
            double saltPerAddition = additionalSaltNeeded / numberOfAdditions;

            instructions.Add($"Tổng lượng muối cần thêm: {additionalSaltNeeded:F2} kg.");
            instructions.Add($"Số lần thêm muối: {numberOfAdditions}.");
            instructions.Add($"Lượng muối mỗi lần: {saltPerAddition:F2} kg.");
            instructions.Add($"Quy tắc 1: Tăng tối đa 0.05% ({maxSaltIncreasePerTime:F2} kg) mỗi lần.");

            // Quy tắc 2: Thời gian giữa các lần tăng là 6-8 giờ
            instructions.Add("Quy tắc 2: Chờ 6-8 giờ giữa các lần thêm muối để đảm bảo an toàn cho cá.");
            for (int i = 1; i <= numberOfAdditions; i++)
            {
                instructions.Add($"Lần {i}: Thêm {saltPerAddition:F2} kg muối. Chờ 6-8 giờ trước khi thêm lần tiếp theo.");
            }

            return new SaltAdditionProcessResponse
            {
                PondId = pondId,
                Instructions = instructions
            };
        }



    }
}