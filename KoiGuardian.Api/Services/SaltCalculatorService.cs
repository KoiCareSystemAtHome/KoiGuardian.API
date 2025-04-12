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
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "Không tìm thấy hồ." }
                };
            }

            // Calculate current volume based on water change percentage
            double currentVolume;
            var additionalNotes = new List<string>();

            if (request.WaterChangePercent < 0 || request.WaterChangePercent > 100)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "WaterChangePercent phải nằm trong khoảng từ 0 đến 100." }
                };
            }

            // Interpret WaterChangePercent as the percentage of water remaining
            currentVolume = Math.Round(pond.MaxVolume * (request.WaterChangePercent / 100), 2);
            additionalNotes.Add($"Thể tích hiện tại của hồ: {currentVolume:F2} lít (dựa trên {request.WaterChangePercent}% của dung tích tối đa {pond.MaxVolume:F2} lít).");

            if (currentVolume <= 0)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "Hồ không có nước, không thể tính toán." }
                };
            }

            // Fetch salt parameter
            var saltParameterQuery = _pondParamRepository.GetQueryable(p => p.Parameter.Name.ToLower() == "salt")
                .Include(p => p.Parameter);
            var saltParameter = await saltParameterQuery.FirstOrDefaultAsync();

            if (saltParameter?.Parameter == null)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "Không tìm thấy thông số muối trong các thông số tiêu chuẩn." }
                };
            }

            // Fetch current salt amount (in kg, not a concentration)
            var currentSaltQuery = _pondParamRepository.GetQueryable(
                p => p.PondId == request.PondId && p.Parameter.ParameterID == saltParameter.Parameter.ParameterID)
                .Include(p => p.Parameter);
            var currentSaltValue = await currentSaltQuery.FirstOrDefaultAsync();
            double currentSaltWeightKg = Math.Round(currentSaltValue?.Value ?? 0, 2); // Directly use the value as kg

            // Calculate the concentration (in kg/L) for water replacement calculations
            double currentSaltConcentrationKgPerL = currentVolume > 0 ? currentSaltWeightKg / currentVolume : 0;

            // Fetch standard salt percentage
            if (!_standardSaltPercentDict.TryGetValue(request.StandardSaltLevel.ToLower(), out double standardSalt))
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "Mức muối tiêu chuẩn không hợp lệ. Giá trị được chấp nhận: Low, Medium, High." }
                };
            }

           
            if (request.StandardSaltLevel.ToLower() == "high")
            {
                additionalNotes.Add("Nếu có cá bệnh truyền nhiễm, nên tách hồ để tránh ảnh hưởng đến các con cá khác.");
            }

            // Calculate additional salt percentage due to fish diseases
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

            // Calculate the required salt percentage (standard + modification due to fish conditions)
            double requiredSaltPercent = standardSalt + saltModifyPercent;

            // Calculate the optimal salt range (lower and upper bounds)
            double lowerSaltPercent = standardSalt; // Lower bound of the standard
            double upperSaltPercent = standardSalt; // Upper bound of the standard

            // Adjust the bounds based on the salt parameter's standard range (if available)
            if (saltParameter.Parameter.WarningLowwer.HasValue)
            {
                lowerSaltPercent = saltParameter.Parameter.WarningLowwer.Value + saltModifyPercent;
            }
            if (saltParameter.Parameter.WarningUpper.HasValue)
            {
                upperSaltPercent = saltParameter.Parameter.WarningUpper.Value + saltModifyPercent;
            }

            // Debug the percentages
            Console.WriteLine($"lowerSaltPercent: {lowerSaltPercent:F4}%");
            Console.WriteLine($"upperSaltPercent: {upperSaltPercent:F4}%");

            // Calculate the optimal salt range in kg
            double lowerSaltWeightKg = Math.Round(currentVolume * (lowerSaltPercent / 100), 2);
            double upperSaltWeightKg = Math.Round(currentVolume * (upperSaltPercent / 100), 2);

            // Debug the optimal range
            Console.WriteLine($"currentVolume: {currentVolume:F2}L");
            Console.WriteLine($"lowerSaltWeightKg: {lowerSaltWeightKg:F2}kg");
            Console.WriteLine($"upperSaltWeightKg: {upperSaltWeightKg:F2}kg");

            // Calculate target salt weight (midpoint of the optimal range for simplicity)
            double targetSaltWeightKg = Math.Round((lowerSaltWeightKg + upperSaltWeightKg) / 2, 2);

            // Initialize variables
            double saltDifference = Math.Round(targetSaltWeightKg - currentSaltWeightKg, 2);
            double additionalSaltNeeded = 0.0;
            double excessSalt = 0.0;
            double waterToReplace = 0.0;

            // Check if current salt is within the optimal range
            if (currentSaltWeightKg >= lowerSaltWeightKg && currentSaltWeightKg <= upperSaltWeightKg)
            {
                additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) đã nằm trong khoảng tối ưu ({lowerSaltWeightKg:F2} kg - {upperSaltWeightKg:F2} kg).");
                additionalSaltNeeded = 0.0;
                excessSalt = 0.0;
                waterToReplace = 0.0;
            }
            else if (currentSaltWeightKg < lowerSaltWeightKg)
            {
                additionalSaltNeeded = saltDifference > 0 ? saltDifference : 0.0;
                additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) thấp hơn mức tối ưu ({lowerSaltWeightKg:F2} kg - {upperSaltWeightKg:F2} kg).");
                additionalNotes.Add($"Cần thêm: {additionalSaltNeeded:F2} kg muối để đạt mức mục tiêu.");
            }
            else if (currentSaltWeightKg > upperSaltWeightKg)
            {
                excessSalt = Math.Abs(saltDifference);
                double saltToRemove = currentSaltWeightKg - upperSaltWeightKg;
                if (currentSaltConcentrationKgPerL > 0)
                {
                    waterToReplace = saltToRemove / currentSaltConcentrationKgPerL;
                    waterToReplace = Math.Round(waterToReplace, 2);

                    // Debug the water replacement calculation
                    Console.WriteLine($"saltToRemove: {saltToRemove:F2}kg");
                    Console.WriteLine($"currentSaltConcentrationKgPerL: {currentSaltConcentrationKgPerL:F6} kg/L");
                    Console.WriteLine($"waterToReplace: {waterToReplace:F2}L");

                    // Check if the water to replace exceeds the current volume
                    if (waterToReplace > currentVolume)
                    {
                        additionalNotes.Add($"Lỗi: Lượng nước cần thay ({waterToReplace:F2} lít) vượt quá lượng nước trong hồ ({currentVolume:F2} lít).");
                        waterToReplace = currentVolume; // Cap at the current volume
                    }

                    additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) cao hơn mức tối ưu ({lowerSaltWeightKg:F2} kg - {upperSaltWeightKg:F2} kg).");
                    additionalNotes.Add($"Cần thay {waterToReplace:F2} lít nước (rút ra và thêm nước lọc) để đưa muối về mức tối ưu.");
                }
                else
                {
                    additionalNotes.Add("Không thể tính lượng nước cần thay vì lượng muối hiện tại bằng 0.");
                }
            }

            // Threshold calculations (in mg/L)
            double currentSaltConcentrationMgPerL = (currentSaltConcentrationKgPerL * 1_000_000); // kg/L to mg/L
            double? warningLowerMgPerL = saltParameter.Parameter.WarningLowwer.HasValue
                ? Math.Round((saltParameter.Parameter.WarningLowwer.Value * 1_000_000) / currentVolume, 2)
                : null;
            double? warningUpperMgPerL = saltParameter.Parameter.WarningUpper.HasValue
                ? Math.Round((saltParameter.Parameter.WarningUpper.Value * 1_000_000) / currentVolume, 2)
                : null;
            double? dangerLowerMgPerL = saltParameter.Parameter.DangerLower.HasValue
                ? Math.Round((saltParameter.Parameter.DangerLower.Value * 1_000_000) / currentVolume, 2)
                : null;
            double? dangerUpperMgPerL = saltParameter.Parameter.DangerUpper.HasValue
                ? Math.Round((saltParameter.Parameter.DangerUpper.Value * 1_000_000) / currentVolume, 2)
                : null;

            double additionalWaterNeeded = 0.0;
            List<string> thresholdMessages = new List<string>();
            if (currentSaltWeightKg < lowerSaltWeightKg || currentSaltWeightKg > upperSaltWeightKg)
            {
                var (waterNeeded, messages) = await CalculateWaterAdjustmentAndThresholds(
                    pond, currentSaltConcentrationMgPerL, targetSaltWeightKg, currentVolume,
                    warningLowerMgPerL, warningUpperMgPerL, dangerLowerMgPerL, dangerUpperMgPerL);
                additionalWaterNeeded = waterNeeded;
                thresholdMessages = messages;
            }

            additionalNotes.AddRange(thresholdMessages);

            if (excessSalt > 0 && waterToReplace == 0 && (currentSaltWeightKg < lowerSaltWeightKg || currentSaltWeightKg > upperSaltWeightKg))
            {
                additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) vượt quá mục tiêu ({targetSaltWeightKg:F2} kg).");
                additionalNotes.Add($"Lượng muối dư: {excessSalt:F2} kg.");
                if (currentVolume + additionalWaterNeeded > pond.MaxVolume)
                {
                    double excessVolume = Math.Round((currentVolume + additionalWaterNeeded) - pond.MaxVolume, 2);
                    additionalNotes.Add($"Cảnh báo: Thêm {additionalWaterNeeded:F2} lít nước sẽ vượt quá dung tích hồ {excessVolume:F2} lít.");
                }
            }
            else if (additionalSaltNeeded > 0 && waterToReplace == 0)
            {
                additionalNotes.Add($"Cần thêm: {additionalSaltNeeded:F2} kg muối.");
                additionalNotes.Add($"Lượng muối hiện tại: {currentSaltWeightKg:F2} kg.");
                additionalNotes.Add($"Lượng muối mục tiêu: {targetSaltWeightKg:F2} kg.");
            }

            // Suggested reminders for adding salt
            var suggestedReminders = new List<SuggestedSaltReminderResponse>();
            if (additionalSaltNeeded > 0 && waterToReplace == 0)
            {
                int numberOfAdditions = additionalSaltNeeded <= 0.5 ? 2 : 3;
                double saltPerAddition = Math.Round(additionalSaltNeeded / numberOfAdditions, 2);

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

            // Prepare response
            var response = new CalculateSaltResponse
            {
                PondId = request.PondId,
                TotalSalt = targetSaltWeightKg,
                CurrentSalt = currentSaltWeightKg,
                SaltNeeded = additionalSaltNeeded,
                ExcessSalt = excessSalt,
                WaterNeeded = Math.Round(waterToReplace, 2),
                AdditionalInstruction = additionalNotes,
                OptimalSaltFrom = lowerSaltWeightKg,
                OptimalSaltTo = upperSaltWeightKg
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