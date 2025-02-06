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
        double GetCurrentSaltLevel(Guid pondId);

        TimeSpan CalculateRemainingTimeToAddSalt(Guid pondId, double saltAmountToAdd);
    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;
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
                    response.Messages.Add($"Phải đợi ít nhất {MIN_HOURS_BETWEEN_ADDITIONS} giờ giữa các lần đổ muối. " +
                                           $"Có thể đổ muối tiếp sau: {response.NextAllowedTime:HH:mm:ss}");
                    return response;
                }
            }

            // Tính toán lượng muối được phép thêm
            double currentSaltLevel = GetCurrentSaltLevel(request.PondId);
            double neededIncrease = request.TargetSaltLevel - currentSaltLevel;

            if (neededIncrease <= 0)
            {
                response.CanAddSalt = false;
                response.Messages.Add($"Nồng độ muối hiện tại ({currentSaltLevel}%) đã đạt hoặc vượt mục tiêu ({request.TargetSaltLevel}%).");
                return response;
            }

            // Kiểm tra xem lượng muối cần thêm có vượt quá giới hạn hay không
            if (neededIncrease > MAX_SALT_INCREASE)
            {
                response.CanAddSalt = true;
                response.AllowedSaltAmount = MAX_SALT_INCREASE;
                response.Messages.Add($"Lượng muối cần thêm vượt quá giới hạn {MAX_SALT_INCREASE}%. Bạn chỉ có thể thêm tối đa {MAX_SALT_INCREASE}% muối một lần.");
            }
            else
            {
                response.CanAddSalt = true;
                response.AllowedSaltAmount = neededIncrease;
                response.Messages.Add($"Có thể thêm {response.AllowedSaltAmount}% muối.");
            }

            return response;
        }


        public void RecordSaltAddition(Guid pondId, double saltAmount)
        {
            var record = new SaltAdditionRecord
            {
                PondId = pondId,
                SaltAmount = saltAmount,
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

        public double GetCurrentSaltLevel(Guid pondId)
        {
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
            var pond =  _pondRepository.GetQueryable(
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
            double pondVolume = pond.MaxVolume; // Thể tích hồ (lít)
            double requiredSaltPercent = standardSalt + 0.01; // Tỷ lệ muối tiêu chuẩn


            double currentVolume = request.WaterChangePercent > 0
               ? pondVolume - (pondVolume * (request.WaterChangePercent / 100))
               : pondVolume;

            // Tính lượng muối dựa trên thể tích nước hiện tại
            double totalSaltWeightKg = currentVolume * (requiredSaltPercent / 100) ;

            double totalSaltWeightMg = currentVolume * (requiredSaltPercent / 100) * 1000;
            double saltConcentrationMgPerL = totalSaltWeightMg / currentVolume;



            // Kiểm tra các giới hạn trong Parameter
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
                TotalSalt = totalSaltWeightKg,
                AdditionalInstruction = notes
            };

        }

       


    }
}