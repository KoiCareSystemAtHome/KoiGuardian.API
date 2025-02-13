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

        private readonly Dictionary<string, double> _standardSaltPercentDict = new()
        {
            { "Low", 0.3 },
            { "Medium", 0.5 },
            { "High", 0.7 }
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

            // Lấy thông tin tham số muối
            var saltParameter = await _pondParamRepository.GetAsync(
                p => p.Parameter.Name.ToLower() == "salt",
                include: p => p.Include(p => p.Parameter)
            );

            if (saltParameter?.Parameter == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Salt parameter not found in standard parameters." }
                };
            }

            // Lấy giá trị muối hiện tại trong ao
            var currentSaltValue = await _pondParamRepository.GetAsync(
                p => p.PondId == request.PondId &&
                     p.Parameter.ParameterID == saltParameter.Parameter.ParameterID,
                include: u => u.Include(p => p.Parameter)
            );

            double currentVolume = request.WaterChangePercent > 0
                ? pond.MaxVolume * (1 - request.WaterChangePercent / 100)
                : pond.MaxVolume;

            double currentSaltConcentration = currentSaltValue?.Value ?? 0; // kg/L

            if (!_standardSaltPercentDict.TryGetValue(request.StandardSaltLevel, out double standardSalt))
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Invalid StandardSaltLevel. Accepted values: Low, Medium, High" }
                };
            }

            // Công thức tính mới
            double requiredSaltPercent = standardSalt + 0.01;
            double totalSaltWeightKg = currentVolume * (requiredSaltPercent / 100);

            double additionalSaltNeeded = totalSaltWeightKg - currentSaltConcentration;

            double totalSaltWeightMg = totalSaltWeightKg * 1000;
            double saltConcentrationMgPerL = totalSaltWeightMg / currentVolume;


            var additionalNotes = new List<string>();

            if (additionalSaltNeeded < 0)
            {
                additionalNotes.Add($"Current salt level ({currentSaltConcentration:F2} kg) is higher than standard ({totalSaltWeightKg:F2} kg).");
                additionalNotes.Add("Consider water change to reduce salt concentration.");

                // Calculate amount of water needed to dilute the salt
                double excessSalt = Math.Abs(additionalSaltNeeded);
                double waterNeeded = excessSalt / (requiredSaltPercent / 100);  // How much water is needed to dilute the excess salt
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = totalSaltWeightKg,
                    WaterNeeded = waterNeeded,
                    AdditionalInstruction = additionalNotes
                };
            }
            else if (additionalSaltNeeded > 0)
            {
                additionalNotes.Add($"Additional salt needed: {additionalSaltNeeded:F2} kg.");
                additionalNotes.Add($"Current salt: {currentSaltConcentration:F2} kg.");
                additionalNotes.Add($"Target salt: {totalSaltWeightKg:F2} kg.");
            }

            if (saltConcentrationMgPerL <  saltParameter.Parameter.WarningLowwer)
                additionalNotes.Add("Lượng muối dưới mức cảnh báo.");
            if (saltConcentrationMgPerL > saltParameter.Parameter.WarningUpper)
                additionalNotes.Add("Salt level exceeds the warning threshold.");

            if (saltConcentrationMgPerL < saltParameter.Parameter.DangerLower)
                additionalNotes.Add("Salt level is below the danger threshold. Fish may be at risk.");
            if (saltConcentrationMgPerL > (saltParameter.Parameter.DangerUpper ?? double.MaxValue))
                additionalNotes.Add("Salt level exceeds the danger threshold. Fish may be at risk.");

            return new CalculateSaltResponse
            {
                PondId = pond.PondID,
                TotalSalt = totalSaltWeightKg,
                CurrentSalt = currentSaltConcentration,
                SaltNeeded = additionalSaltNeeded,
                AdditionalInstruction = additionalNotes
            };
        }



    }
}