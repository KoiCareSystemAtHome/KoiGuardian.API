using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IRepository<KoiDiseaseProfile> _koiDiseaseProfileRepository;
        private static readonly ConcurrentDictionary<Guid, List<SaltAdditionRecord>> _saltHistory = new();

        private const double MAX_SALT_INCREASE_PERCENT = 0.05; // 0.05% maximum increase
        private const int MIN_HOURS_BETWEEN_ADDITIONS = 6;
        private const int MAX_HOURS_BETWEEN_ADDITIONS = 8;

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


        public async Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request)
        {
            if (request.PondId == Guid.Empty)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Invalid PondId provided." }
                };
            }

            // Get pond details
            var pond = await _pondRepository.GetAsync(
                u => u.PondID == request.PondId,
                include: u => u.Include(p => p.Fish)
            );

            if (pond == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Pond not found." }
                };
            }

            // Other validations remain the same...
            var additionalNotes = new List<string>();

            if (!_standardSaltPercentDict.TryGetValue(request.StandardSaltLevel, out double standardSalt))
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Invalid StandardSaltLevel. Accepted values: Low, Medium, High" }
                };
            }

            var saltParameter = await _pondParamRepository.GetAsync(
                u => u.PondId == pond.PondID && u.Parameter.Name.ToLower() == "salt",
                include: u => u.Include(p => p.Parameter)
            );

            // Calculate required salt percentage
            double requiredSaltPercent = standardSalt + 0.01;

            double currentVolume = request.WaterChangePercent > 0
                ? pond.MaxVolume - (pond.MaxVolume * (request.WaterChangePercent / 100))
                : pond.MaxVolume;

            double targetSaltWeightKg = currentVolume * (requiredSaltPercent / 100);

            double additionalWaterNeeded = 0;

            // Handle salt reduction scenario
            if (request.IsReducingSalt && request.CurrentSaltAmount.HasValue)
            {
                double currentSaltPercent = (request.CurrentSaltAmount.Value / currentVolume) * 100;

                if (currentSaltPercent > requiredSaltPercent)
                {
                    double targetVolume = (request.CurrentSaltAmount.Value / requiredSaltPercent) * 100;
                    additionalWaterNeeded = targetVolume - currentVolume;

                    if (targetVolume > pond.MaxVolume)
                    {
                        additionalNotes.Add($"Không thể giảm muối về mức mục tiêu do đã đạt giới hạn thể tích hồ.");
                        additionalNotes.Add($"Cần xem xét phương án thay nước.");

                        double achievablePercent = (request.CurrentSaltAmount.Value / pond.MaxVolume) * 100;
                        additionalNotes.Add($"Nồng độ muối tối thiểu có thể đạt được: {achievablePercent:F2}%");
                    }
                    else
                    {
                        additionalNotes.Add($"Cần thêm {additionalWaterNeeded:F2} lít nước để đạt nồng độ muối mục tiêu {requiredSaltPercent:F2}%");
                        additionalNotes.Add($"Nồng độ muối hiện tại: {currentSaltPercent:F2}%");
                    }

                    return new CalculateSaltResponse
                    {
                        PondId = pond.PondID,
                        TotalSalt = request.CurrentSaltAmount.Value,
                        ExcessSalt = request.CurrentSaltAmount.Value - targetSaltWeightKg,
                        WaterNeeded = additionalWaterNeeded,
                        AdditionalInstruction = additionalNotes
                    };
                }
            }

            // Continue with normal salt calculation...
            double totalSaltWeightKg = currentVolume * (requiredSaltPercent / 100);
            double totalSaltWeightMg = totalSaltWeightKg * 1000;
            double saltConcentrationMgPerL = totalSaltWeightMg / currentVolume;

            var parameter = saltParameter.Parameter;

            if (saltConcentrationMgPerL < (parameter.WarningLowwer ?? 0))
                additionalNotes.Add("Lượng muối dưới mức cảnh báo.");

            if (saltConcentrationMgPerL > (parameter.WarningUpper ?? double.MaxValue))
                additionalNotes.Add("Lượng muối vượt mức cảnh báo.");

            if (saltConcentrationMgPerL < (parameter.DangerLower ?? 0))
                additionalNotes.Add("Lượng muối dưới mức nguy hiểm. Cá có thể gặp nguy hiểm.");

            if (saltConcentrationMgPerL > (parameter.DangerUpper ?? double.MaxValue))
                additionalNotes.Add("Lượng muối vượt mức nguy hiểm. Cá có thể gặp nguy hiểm.");

            return new CalculateSaltResponse
            {
                PondId = pond.PondID,
                TotalSalt = totalSaltWeightKg,
                WaterNeeded = additionalWaterNeeded,
                AdditionalInstruction = additionalNotes
            };
        }

    }
}