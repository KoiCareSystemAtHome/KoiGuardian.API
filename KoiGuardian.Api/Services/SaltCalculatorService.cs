using System;
using System.Collections.Concurrent;
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

        private const double MAX_SALT_INCREASE = 0.05;
        private const int MIN_HOURS_BETWEEN_ADDITIONS = 6;


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

            // Lấy lịch sử đổ muối của hồ
            _saltHistory.TryGetValue(request.PondId, out var history);
            var lastAddition = history?.OrderByDescending(h => h.AddedTime).FirstOrDefault();

            // Kiểm tra thời gian giữa các lần đổ muối
            if (lastAddition != null)
            {
                var hoursSinceLastAddition = (DateTime.UtcNow - lastAddition.AddedTime).TotalHours;

                if (hoursSinceLastAddition < MIN_HOURS_BETWEEN_ADDITIONS)
                {
                    response.CanAddSalt = false;
                    response.NextAllowedTime = lastAddition.AddedTime.AddHours(MIN_HOURS_BETWEEN_ADDITIONS);
                    response.Messages.Add($"Phải đợi ít nhất {MIN_HOURS_BETWEEN_ADDITIONS} giờ giữa các lần đổ muối.");
                    return response;
                }
            }

            // Tính toán lượng muối được phép thêm
            double maxAllowedSaltWeightKg = request.TargetSaltWeightKg * MAX_SALT_INCREASE;
            double neededSaltWeightKg = request.TargetSaltWeightKg - currentSaltWeightKg;

            if (neededSaltWeightKg <= 0)
            {
                response.CanAddSalt = false;
                response.Messages.Add("Đã đạt hoặc vượt mức muối mục tiêu.");
                return response;
            }

            response.CanAddSalt = true;
            response.AllowedSaltWeightKg = Math.Min(neededSaltWeightKg, maxAllowedSaltWeightKg);
            response.Messages.Add($"Có thể thêm {response.AllowedSaltWeightKg} kg muối.");

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

            if (pond.MaxVolume <= 0)
            {
                return new CalculateSaltResponse
                {
                    PondId = pond.PondID,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Invalid Pond Volume." }
                };
            }

            if (pond.Fish == null || !pond.Fish.Any())
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "No fish found in this pond." }
                };
            }

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

            // Retrieve salt parameter for pond
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
                    AdditionalInstruction = new List<string> { "Salt parameter not found for this pond." }
                };
            }

            /*bool hasSickFish = false;
            foreach (var koi in pond.Fish)
            {
                try
                {
                    var treatmentAmount = await _koiDiseaseProfileRepository.GetAsync(
                        u => koi.KoiID == koi.KoiID && u.EndDate <= DateTime.Now && u.Disease != null,
                        include: u => u.Include(d => d.Disease)
                    );

                    if (treatmentAmount?.Disease != null)
                    {
                        hasSickFish = true;
                        additionalNotes.Add($"{koi.Name} đang điều trị bệnh {treatmentAmount.Disease.Name}");
                    }
                }
                catch
                {
                    // If there's an error getting treatment info, skip it and continue
                    continue;
                }
            }*/

            // Nếu có cá bệnh, tăng tỷ lệ muối thêm 0.01
            double requiredSaltPercent = standardSalt + 0.01 /*(hasSickFish ? 0.02 : 0.01)*/;

            
            double currentVolume = request.WaterChangePercent > 0
                ? pond.MaxVolume - (pond.MaxVolume * (request.WaterChangePercent / 100))
                : pond.MaxVolume;

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
                AdditionalInstruction = additionalNotes
            };
        }

    }
}