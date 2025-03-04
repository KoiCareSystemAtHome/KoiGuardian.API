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
        AddSaltResponse ValidateAndCalculateSaltAddition(AddSaltRequest request);
        void RecordSaltAddition(Guid pondId, double saltAmount);
        double GetCurrentSaltWeightKg(Guid pondId);

        TimeSpan CalculateRemainingTimeToAddSalt(Guid pondId, double saltAmountToAdd);
    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;
        private readonly IRepository<PondStandardParam> _pondStandardParamRepository;
        private readonly IRepository<KoiDiseaseProfile> _koiDiseaseProfileRepository;
        private static readonly ConcurrentDictionary<Guid, List<SaltAdditionRecord>> _saltHistory = new();

        private const double MAX_SALT_INCREASE_PERCENT = 0.05; // 0.05% maximum increase
        private const int MIN_HOURS_BETWEEN_ADDITIONS = 6;
        private const int MAX_HOURS_BETWEEN_ADDITIONS = 8;

       

        public SaltCalculatorService(
            IRepository<Pond> pondRepository,
            IRepository<KoiDiseaseProfile> koiDiseaseProfile,
            IRepository<RelPondParameter> pondParamRepository)
            
        {
            _pondRepository = pondRepository;
            _pondParamRepository = pondParamRepository;
            _koiDiseaseProfileRepository = koiDiseaseProfile;
        }
        public AddSaltResponse ValidateAndCalculateSaltAddition(AddSaltRequest request)
        {
            var response = new AddSaltResponse();
            var currentSaltWeightKg = GetCurrentSaltWeightKg(request.PondId);

            // Get pond volume
            var pond = _pondRepository.GetQueryable(p => p.PondID == request.PondId).FirstOrDefault();
            if (pond == null)
            {
                response.CanAddSalt = false;
                response.Messages.Add("Không tìm thấy thông tin hồ.");
                return response;
            }

            // Get salt addition history
            _saltHistory.TryGetValue(request.PondId, out var history);
            var lastAddition = history?.OrderByDescending(h => h.AddedTime).FirstOrDefault();

            // Check time between additions
            if (lastAddition != null)
            {
                var hoursSinceLastAddition = (DateTime.UtcNow - lastAddition.AddedTime).TotalHours;

                if (hoursSinceLastAddition < MIN_HOURS_BETWEEN_ADDITIONS)
                {
                    response.CanAddSalt = false;
                    response.NextAllowedTime = lastAddition.AddedTime.AddHours(MIN_HOURS_BETWEEN_ADDITIONS);
                    response.Messages.Add($"Phải đợi ít nhất {MIN_HOURS_BETWEEN_ADDITIONS} giờ giữa các lần đổ muối. " +
                        $"Có thể đổ muối tiếp sau {response.NextAllowedTime:HH:mm:ss}");
                    return response;
                }

                if (hoursSinceLastAddition > MAX_HOURS_BETWEEN_ADDITIONS)
                {
                    response.Messages.Add("Đã quá 8 giờ từ lần đổ muối trước, nên đổ muối ngay để đảm bảo hiệu quả.");
                }
            }

            // Calculate maximum allowed salt addition (0.05% of current volume)
            double currentVolumeL = pond.MaxVolume;
            double maxAllowedSaltIncreaseKg = (currentVolumeL * MAX_SALT_INCREASE_PERCENT) / 100;
            double neededSaltWeightKg = request.TargetSaltWeightKg - currentSaltWeightKg;

            if (neededSaltWeightKg <= 0)
            {
                response.CanAddSalt = false;
                response.Messages.Add("Đã đạt hoặc vượt mức muối mục tiêu.");
                return response;
            }

            response.CanAddSalt = true;
            response.AllowedSaltWeightKg = Math.Min(neededSaltWeightKg, maxAllowedSaltIncreaseKg);

            if (response.AllowedSaltWeightKg < neededSaltWeightKg)
            {
                response.Messages.Add($"Có thể thêm tối đa {response.AllowedSaltWeightKg:F2} kg muối (0.05% thể tích hồ).");
                response.Messages.Add($"Cần thêm {Math.Ceiling(neededSaltWeightKg / maxAllowedSaltIncreaseKg)} lần để đạt mục tiêu.");
            }
            else
            {
                response.Messages.Add($"Có thể thêm {response.AllowedSaltWeightKg:F2} kg muối để đạt mục tiêu.");
            }

            return response;
        }

        public void RecordSaltAddition(Guid pondId, double saltWeightKg)
        {
            var record = new SaltAdditionRecord
            {
                PondId = pondId,
                SaltAmount = saltWeightKg,
                AddedTime = DateTime.UtcNow
            };

            _saltHistory.AddOrUpdate(
                pondId,
                new List<SaltAdditionRecord> { record },
                (_, list) =>
                {
                    list.Add(record);
                    return list;
                });
        }

        public double GetCurrentSaltWeightKg(Guid pondId)
        {
            // Lấy từ history nếu có
            if (_saltHistory.TryGetValue(pondId, out var history))
            {
                return history.Sum(h => h.SaltAmount);
            }
            return 0;
        }



        public TimeSpan CalculateRemainingTimeToAddSalt(Guid pondId, double saltAmountToAdd)
        {
            // Tốc độ đổ muối, ví dụ: 0.5 kg muối mỗi giờ
            const double saltDischargeRatePerHour = 0.5; // kg muối mỗi giờ

            // Lấy thể tích hồ (lít) từ thông tin hồ
            var pond = _pondRepository.GetQueryable(
                    predicate: p => p.PondID == pondId


                );
            if (pond == null)
            {
                throw new Exception("Pond not found");
            }

            // Tính toán thời gian còn lại để đổ hết lượng muối
            double totalSaltWeightKg = saltAmountToAdd; // Lượng muối cần đổ (kg)

            // Tính thời gian cần để đổ hết lượng muối (theo giờ)
            double hoursRemaining = totalSaltWeightKg / saltDischargeRatePerHour;

            return TimeSpan.FromHours(hoursRemaining); // Trả về thời gian còn lại dưới dạng TimeSpan
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

            return new CalculateSaltResponse
            {
                PondId = pond.PondID,
                TotalSalt = targetSaltWeightKg,
                CurrentSalt = currentSaltConcentration,
                SaltNeeded = additionalSaltNeeded,
                AdditionalInstruction = additionalNotes
            };
        }



    }
}