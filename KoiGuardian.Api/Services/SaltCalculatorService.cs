using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface ISaltCalculatorService
    {
        Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request);
        Task<SaltAdditionProcessResponse> GetSaltAdditionProcess(Guid pondId);
        Task<List<PondReminder>> GetSaltReminders(Guid pondId);
   
        Task<bool> UpdateSaltAmount(Guid pondId, double addedSaltKg, CancellationToken cancellationToken);
        Task<bool> SaveSelectedSaltReminders(SaveSaltRemindersRequest request);
        Task<List<SaltReminderRequest>> GenerateSaltAdditionRemindersAsync(Guid pondId, int cycleHours, CancellationToken cancellationToken);
        Task<bool> UpdateSaltReminderDateAsync(UpdateSaltReminderRequest request);
    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;
        private readonly IRepository<PondStandardParam> _pondStandardParamRepository;
        private readonly IRepository<KoiDiseaseProfile> _koiDiseaseProfileRepository;
        private readonly IRepository<PondReminder> _reminderRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        private static readonly ConcurrentDictionary<Guid, CalculateSaltResponse> _saltCalculationCache = new();

        public SaltCalculatorService(
            IRepository<Pond> pondRepository,
            IRepository<KoiDiseaseProfile> koiDiseaseProfile,
            IRepository<PondReminder> reminderRepository,
            IRepository<PondStandardParam> pondStandardParamRepository,
            IRepository<RelPondParameter> pondParamRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _pondRepository = pondRepository;
            _pondParamRepository = pondParamRepository;
            _reminderRepository = reminderRepository;
            _koiDiseaseProfileRepository = koiDiseaseProfile;
            _pondStandardParamRepository = pondStandardParamRepository;
            _unitOfWork = unitOfWork;
        }

        private readonly Dictionary<string, double> _standardSaltPercentDict = new()
        {
            { "low", 0.003 },
            { "medium", 0.005 },
            { "high", 0.007 }
        };

        public async Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request)
        {
            var pondQuery = _pondRepository.GetQueryable(p => p.PondID == request.PondId)
                .Include(p => p.Fish);
            var pond = await pondQuery.FirstOrDefaultAsync();

            if (pond == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Không tìm thấy hồ." }
                };
            }

            double currentVolume = request.WaterChangePercent > 0
                ? pond.MaxVolume * (1 - request.WaterChangePercent / 100)
                : pond.MaxVolume;

            if (request.WaterChangePercent == 100)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,

                    AdditionalInstruction = new List<string> { "Hồ không có nước, không thể tính toán." }
                };
            }
            else if (request.WaterChangePercent > 100)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Mực nước hiện tại không hợp lý, vui lòng kiểm tra lại." }

                };
            }

            var saltParameterQuery = _pondParamRepository.GetQueryable(p => p.Parameter.Name.ToLower() == "salt")
                .Include(p => p.Parameter);
            var saltParameter = await saltParameterQuery.FirstOrDefaultAsync();

            if (saltParameter?.Parameter == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Không tìm thấy thông số muối trong các thông số tiêu chuẩn." }
                };
            }

            var currentSaltQuery = _pondParamRepository.GetQueryable(
                p => p.PondId == request.PondId && p.Parameter.ParameterID == saltParameter.Parameter.ParameterID)
                .Include(p => p.Parameter);
            var currentSaltValue = await currentSaltQuery.FirstOrDefaultAsync();
            double currentSaltConcentration = currentSaltValue?.Value ?? 0;

            if (!_standardSaltPercentDict.TryGetValue(request.StandardSaltLevel.ToLower(), out double standardSalt))
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0,
                    AdditionalInstruction = new List<string> { "Mức muối tiêu chuẩn không hợp lệ. Giá trị được chấp nhận: Low, Medium, High." }
                };
            }

            var additionalNotes = new List<string>();
            if (request.StandardSaltLevel.ToLower() == "high")
            {
                additionalNotes.Add("Nếu có cá bệnh truyền nhiễm, nên tách hồ để tránh ảnh hưởng đến các con cá khác.");
            }

            double saltModifyPercent = 0;
            var fishList = pond.Fish?.ToList() ?? new List<Fish>();
            if (fishList.Any())
            {
                foreach (var fish in fishList)
                {
                    var diseaseProfilesQuery = _koiDiseaseProfileRepository.GetQueryable(d => d.FishId == fish.KoiID)
                        .Include(d => d.Disease);
                    var diseaseProfiles = await diseaseProfilesQuery.ToListAsync();

                    foreach (var diseaseProfile in diseaseProfiles)
                    {
                        if (diseaseProfile.Status == ProfileStatus.Accept || diseaseProfile.Status == ProfileStatus.Pending)
                        {
                            if (diseaseProfile.Disease != null)
                            {
                                saltModifyPercent += diseaseProfile.Disease.SaltModifyPercent;
                                additionalNotes.Add($"Cá {fish.Name} mắc bệnh '{diseaseProfile.Disease.Name}', ảnh hưởng đến mức muối {diseaseProfile.Disease.SaltModifyPercent}%.");
                            }
                        }
                    }
                }
            }
            else
            {
                additionalNotes.Add("Không tìm thấy cá trong hồ.");
            }

            double requiredSaltPercent = standardSalt + saltModifyPercent;

            double targetSaltWeightKg = currentVolume * requiredSaltPercent;
            double additionalSaltNeeded = targetSaltWeightKg - currentSaltConcentration;


            double? warningLowerMgPerL = saltParameter.Parameter.WarningLowwer.HasValue
                ? (saltParameter.Parameter.WarningLowwer * 1_000_000) / currentVolume
                : null;
            double? warningUpperMgPerL = saltParameter.Parameter.WarningUpper.HasValue
                ? (saltParameter.Parameter.WarningUpper * 1_000_000) / currentVolume
                : null;
            double? dangerLowerMgPerL = saltParameter.Parameter.DangerLower.HasValue
                ? (saltParameter.Parameter.DangerLower * 1_000_000) / currentVolume
                : null;
            double? dangerUpperMgPerL = saltParameter.Parameter.DangerUpper.HasValue
                ? (saltParameter.Parameter.DangerUpper * 1_000_000) / currentVolume
                : null;

            var (additionalWaterNeeded, thresholdMessages) = await CalculateWaterAdjustmentAndThresholds(
                pond, currentSaltConcentration, targetSaltWeightKg, currentVolume,
                warningLowerMgPerL, warningUpperMgPerL, dangerLowerMgPerL, dangerUpperMgPerL);

            additionalNotes.AddRange(thresholdMessages);

            if (additionalSaltNeeded < 0)
            {
                additionalNotes.Add($"Lượng muối hiện tại ({currentSaltConcentration:F2} kg) vượt quá mục tiêu ({targetSaltWeightKg:F2} kg).");
                if (currentVolume + additionalWaterNeeded > pond.MaxVolume)
                {
                    double excessVolume = (currentVolume + additionalWaterNeeded) - pond.MaxVolume;
                    additionalNotes.Add($"Cảnh báo: Thêm {additionalWaterNeeded:F2} lít nước sẽ vượt quá dung tích hồ {excessVolume:F2} lít.");
                }
            }
            else if (additionalSaltNeeded > 0)
            {
                additionalNotes.Add($"Cần thêm: {additionalSaltNeeded:F2} kg muối.");
                additionalNotes.Add($"Lượng muối hiện tại: {currentSaltConcentration:F2} kg.");
                additionalNotes.Add($"Lượng muối mục tiêu: {targetSaltWeightKg:F2} kg.");
            }

            var suggestedReminders = new List<SuggestedSaltReminderResponse>();
            if (additionalSaltNeeded > 0)
            {
                int numberOfAdditions = additionalSaltNeeded <= 0.5 ? 2 : 3;
                double saltPerAddition = additionalSaltNeeded / numberOfAdditions;

                int hoursInterval = 12;

                DateTime startTime = DateTime.UtcNow;
                for (int i = 0; i < numberOfAdditions; i++)
                {
                    DateTime maintainDate = startTime.AddHours(hoursInterval * i);
                    suggestedReminders.Add(new SuggestedSaltReminderResponse
                    {
                        TemporaryId = Guid.NewGuid(),
                        Title = "Salt Addition Reminder",
                        Description = $"Add {saltPerAddition:F2} kg of salt (Step {i + 1}/{numberOfAdditions}). Total: {additionalSaltNeeded:F2} kg.",
                        MaintainDate = maintainDate.ToUniversalTime()
                    });
                }
            }

            var response = new CalculateSaltResponse
            {
                PondId = request.PondId,
                TotalSalt = targetSaltWeightKg,
                CurrentSalt = currentSaltConcentration,
                SaltNeeded = additionalSaltNeeded,

                WaterNeeded = additionalWaterNeeded,

                AdditionalInstruction = additionalNotes,
                
            };

            _saltCalculationCache[request.PondId] = response;
            return response;
        }

        private async Task<(double additionalWaterNeeded, List<string> messages)> CalculateWaterAdjustmentAndThresholds(
            Pond pond, double currentSaltConcentration, double targetSaltWeightKg, double currentVolume,
            double? warningLowerMgPerL, double? warningUpperMgPerL, double? dangerLowerMgPerL, double? dangerUpperMgPerL)
        {
            var messages = new List<string>();
            double additionalWaterNeeded = 0;

            double saltConcentrationMgPerL = (currentSaltConcentration * 1_000_000) / currentVolume;

            if (currentSaltConcentration > targetSaltWeightKg && targetSaltWeightKg > 0)
            {
                double newTotalVolume = currentSaltConcentration / (targetSaltWeightKg / currentVolume);
                additionalWaterNeeded = newTotalVolume - currentVolume;
            }

            if (warningLowerMgPerL.HasValue && saltConcentrationMgPerL < warningLowerMgPerL)
            {
                messages.Add("Nồng độ muối dưới mức ngưỡng cảnh báo thấp.");
            }
            else if (warningUpperMgPerL.HasValue && dangerUpperMgPerL.HasValue &&
                     saltConcentrationMgPerL > warningUpperMgPerL && saltConcentrationMgPerL <= dangerUpperMgPerL)
            {
                if (additionalWaterNeeded > 0)
                {
                    messages.Add($"Nồng độ muối trên mức ngưỡng cảnh báo cao. Có thể thêm {additionalWaterNeeded:F2} lít nước để cân bằng hồ.");
                }
            }

            if (dangerLowerMgPerL.HasValue && saltConcentrationMgPerL < dangerLowerMgPerL)
            {
                messages.Add("Nồng độ muối dưới mức ngưỡng nguy hiểm thấp. Cá có thể gặp rủi ro.");
            }
            else if (dangerUpperMgPerL.HasValue && saltConcentrationMgPerL > dangerUpperMgPerL)
            {
                if (additionalWaterNeeded > 0)
                {
                    messages.Add($"Nồng độ muối trên mức ngưỡng nguy hiểm cao. Nên thêm {additionalWaterNeeded:F2} lít nước để cân bằng hồ.");
                }
            }

            return (additionalWaterNeeded, messages);
        }

        public async Task<SaltAdditionProcessResponse> GetSaltAdditionProcess(Guid pondId)
        {
            if (!_saltCalculationCache.TryGetValue(pondId, out CalculateSaltResponse saltResponse))
                return new SaltAdditionProcessResponse { PondId = pondId, Instructions = new() { "No salt calculation found. Please calculate salt first." } };

            var pond = await _pondRepository.GetQueryable(p => p.PondID == pondId).FirstOrDefaultAsync();
            if (pond == null)
                return new SaltAdditionProcessResponse { PondId = pondId, Instructions = new() { "Pond not found." } };

            double additionalSaltNeeded = saltResponse.SaltNeeded;
            var instructions = new List<string>();

            if (additionalSaltNeeded <= 0)
            {
                instructions.Add("No additional salt needed or current salt exceeds target.");
                return new SaltAdditionProcessResponse { PondId = pondId, Instructions = instructions };
            }

            int numberOfAdditions = additionalSaltNeeded <= 0.5 ? 2 : 3;
            double saltPerAddition = additionalSaltNeeded / numberOfAdditions;

            instructions.Add($"Tổng lượng muối cần thêm: {additionalSaltNeeded:F2} kg.");
            instructions.Add($"Chia thành {numberOfAdditions} lần, mỗi lần thêm {saltPerAddition:F2} kg.");
            instructions.Add("Bước 1: Thêm muối từng lần, cách nhau 12-24 giờ:");
            for (int i = 0; i < numberOfAdditions; i++)
            {
                instructions.Add($"- Lần {i + 1}: Thêm {saltPerAddition:F2} kg muối.");
            }
            instructions.Add("Bước 2: Quan sát cá sau mỗi lần thêm muối.");
            instructions.Add("Bước 3: Sau khi đạt nồng độ mong muốn, duy trì 5-7 ngày.");

            double currentVolume = pond.MaxVolume;
            int waterChangePercent = 20;
            double waterToReplacePerStep = currentVolume * (waterChangePercent / 100.0);

            instructions.Add($"- Sau đó, giảm dần nồng độ bằng cách thay {waterToReplacePerStep:F2} lít nước ({waterChangePercent}% hồ) mỗi lần, cách nhau 1-2 ngày, cho đến khi muối giảm về mức an toàn.");

            return new SaltAdditionProcessResponse { PondId = pondId, Instructions = instructions };
        }

        public async Task<List<PondReminder>> GetSaltReminders(Guid pondId)
        {
            var remindersQuery = _reminderRepository.GetQueryable(r =>
                r.PondId == pondId &&
                r.ReminderType == ReminderType.Pond &&
                r.Title == "Thêm muối")
                .OrderBy(r => r.MaintainDate);

            return await remindersQuery.ToListAsync();
        }

      
        public async Task<bool> UpdateSaltAmount(Guid pondId, double addedSaltKg, CancellationToken cancellationToken)
        {
            try
            {
                var pond = await _pondRepository.GetQueryable(p => p.PondID == pondId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (pond == null)
                {
                    return false;
                }

                var saltParamQuery = _pondParamRepository.GetQueryable(p =>
                    p.PondId == pondId &&
                    p.Parameter.Name.ToLower() == "salt")
                    .Include(p => p.Parameter);

                var saltParameter = await saltParamQuery.FirstOrDefaultAsync(cancellationToken);
                double currentSaltKg = saltParameter?.Value ?? 0;

                if (saltParameter == null)
                {
                    var standardSaltParam = await _pondStandardParamRepository
                        .GetQueryable(p => p.Name.ToLower() == "salt")
                        .FirstOrDefaultAsync(cancellationToken);

                    if (standardSaltParam == null)
                    {
                        return false;
                    }

                    saltParameter = new RelPondParameter
                    {
                        RelPondParameterId = Guid.NewGuid(),
                        PondId = pondId,
                        ParameterID = standardSaltParam.ParameterID,
                        Value = (float)addedSaltKg,
                        CalculatedDate = DateTime.UtcNow,
                        ParameterHistoryId = Guid.NewGuid()
                    };
                    _pondParamRepository.Insert(saltParameter);
                }
                else
                {
                    saltParameter.Value = (float)(currentSaltKg + addedSaltKg);
                    saltParameter.CalculatedDate = DateTime.UtcNow;
                    _pondParamRepository.Update(saltParameter);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (_saltCalculationCache.TryGetValue(pondId, out CalculateSaltResponse cachedResponse))
                {
                    double updatedSaltAmount = currentSaltKg + addedSaltKg;
                    cachedResponse.CurrentSalt = updatedSaltAmount;
                    cachedResponse.SaltNeeded = Math.Max(0, cachedResponse.TotalSalt - updatedSaltAmount);
                    _saltCalculationCache[pondId] = cachedResponse;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating salt amount: {ex.Message}");
                return false;
            }
        }



        public async Task<List<SaltReminderRequest>> GenerateSaltAdditionRemindersAsync(Guid pondId, int cycleHours, CancellationToken cancellationToken)
        {
            if (cycleHours != 12 && cycleHours != 24)
            {
                throw new ArgumentException("Chu kỳ chỉ có thể là 12 hoặc 24 giờ.");
            }

            if (!_saltCalculationCache.TryGetValue(pondId, out CalculateSaltResponse saltResponse))
            {
                throw new InvalidOperationException("Không tìm thấy thông tin tính toán muối cho hồ này. Vui lòng tính toán muối trước.");
            }

            var pond = await _pondRepository.GetQueryable(p => p.PondID == pondId).FirstOrDefaultAsync(cancellationToken);
            if (pond == null)
            {
                throw new InvalidOperationException("Không tìm thấy hồ với ID đã cung cấp.");
            }

            double additionalSaltNeeded = saltResponse.SaltNeeded;
            if (additionalSaltNeeded <= 0)
            {
                return new List<SaltReminderRequest>();
            }

            int numberOfAdditions = additionalSaltNeeded <= 0.5 ? 2 : 3;
            double saltPerAddition = additionalSaltNeeded / numberOfAdditions;

            List<SaltReminderRequest> reminders = new List<SaltReminderRequest>();
            DateTime startDate = DateTime.UtcNow.AddHours(2); 

            for (int i = 0; i < numberOfAdditions; i++)
            {
                DateTime maintainDate = startDate.AddHours(cycleHours * i);
                reminders.Add(new SaltReminderRequest
                {
                    PondId = pondId,
                    Title = "Thông báo thêm muối",
                    Description = $"Thêm {saltPerAddition:F2} kg muối (Bước {i + 1}/{numberOfAdditions}). Tổng cộng: {additionalSaltNeeded:F2} kg.",
                    MaintainDate = maintainDate.ToUniversalTime()
                });
            }

            return reminders;
        }

        public async Task<bool> SaveSelectedSaltReminders(SaveSaltRemindersRequest request)
        {
            if (request == null || request.Reminders == null || !request.Reminders.Any())
            {
                return false;
            }

            try
            {
                // Xóa các reminder cũ nếu có
                var existingReminders = await GetSaltReminders(request.PondId);
                foreach (var reminder in existingReminders)
                {
                    _reminderRepository.Delete(reminder);
                }

                // Chuyển đổi từ SaltReminderRequest sang PondReminder và lưu
                foreach (var reminderRequest in request.Reminders)
                {
                    var reminder = new PondReminder
                    {
                        PondReminderId = Guid.NewGuid(),
                        PondId = request.PondId,
                        ReminderType = ReminderType.Pond,
                        Title = reminderRequest.Title,
                        Description = reminderRequest.Description,
                        MaintainDate = reminderRequest.MaintainDate.ToUniversalTime(),
                        SeenDate = DateTime.MinValue.ToUniversalTime()
                    };

                    _reminderRepository.Insert(reminder);
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving salt reminders: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateSaltReminderDateAsync(UpdateSaltReminderRequest request)
        {
            if (request == null || request.PondReminderId == Guid.Empty)
            {
                return false;
            }

            var reminder = await _reminderRepository
                .GetQueryable(r => r.PondReminderId == request.PondReminderId)
                .FirstOrDefaultAsync();

            if (reminder == null)
            {
                throw new InvalidOperationException("Không tìm thấy nhắc nhở cần cập nhật.");
            }

            reminder.MaintainDate = request.NewMaintainDate.ToUniversalTime();
            _reminderRepository.Update(reminder);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

    }
}