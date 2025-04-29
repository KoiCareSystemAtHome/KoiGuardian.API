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
using System.Runtime.InteropServices;

namespace KoiGuardian.Api.Services
{
    public interface ISaltCalculatorService
    {
        Task<CalculateSaltResponse> CalculateSalt(CalculateSaltRequest request);
        Task<SaltAdditionProcessResponse> GetSaltAdditionProcess(Guid pondId);
        Task<List<PondReminder>> GetSaltReminders(Guid pondId);

        Task<SaltUpdateResponse> UpdateSaltAmount(Guid pondId, double addedSaltKg, CancellationToken cancellationToken);
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

            // Kiểm tra phần trăm thay nước
            if (request.WaterChangePercent < 0 || request.WaterChangePercent > 100)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "WaterChangePercent phải nằm trong khoảng từ 0 đến 100." }
                };
            }

            // Tính thể tích hiện tại
            double currentVolume = Math.Round(pond.MaxVolume * (request.WaterChangePercent / 100), 2);
            var additionalNotes = new List<string> { $"Thể tích hiện tại của hồ: {currentVolume:F2} lít (dựa trên {request.WaterChangePercent}% của dung tích tối đa {pond.MaxVolume:F2} lít)." };

            if (currentVolume <= 0)
            {
                return new CalculateSaltResponse
                {
                    PondId = request.PondId,
                    TotalSalt = 0.00,
                    AdditionalInstruction = new List<string> { "Hồ không có nước, không thể tính toán." }
                };
            }

            // Lấy thông số muối
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

            // Lấy nồng độ muối hiện tại (ở %)
            var currentSaltQuery = _pondParamRepository.GetQueryable(
                p => p.PondId == request.PondId && p.Parameter.ParameterID == saltParameter.Parameter.ParameterID)
                .Include(p => p.Parameter).OrderByDescending(p => p.CalculatedDate);
            var currentSaltValue = await currentSaltQuery.FirstOrDefaultAsync();
            double currentSaltConcentrationPercent = Math.Round(currentSaltValue?.Value ?? 0, 2);
            double currentSaltWeightKg = Math.Round((currentSaltConcentrationPercent / 100) * currentVolume, 2);

            // Tính nồng độ muối hiện tại
            double currentSaltConcentrationKgPerL = currentVolume > 0 ? currentSaltWeightKg / currentVolume : 0;

            // Lấy phần trăm muối tiêu chuẩn
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

            // Tính phần trăm muối bổ sung do bệnh của cá
            double saltModifyPercent = 0;
            var fishList = pond.Fish?.ToList() ?? new List<Fish>();
            if (fishList.Any())
            {
                foreach (var fish in fishList)
                {
                    var diseaseProfilesQuery = _koiDiseaseProfileRepository
                        .GetQueryable(d => d.FishId == fish.KoiID  && d.EndDate >= DateTime.UtcNow)
                        .Include(d => d.Disease);
                    var diseaseProfiles = await diseaseProfilesQuery.ToListAsync();

                    foreach (var diseaseProfile in diseaseProfiles)
                    {
                        if (diseaseProfile.Status is ProfileStatus.Accept or ProfileStatus.Pending && diseaseProfile.Disease != null)
                        {
                            saltModifyPercent += diseaseProfile.Disease.SaltModifyPercent;
                            additionalNotes.Add($"Cá {fish.Name} mắc bệnh '{diseaseProfile.Disease.Name}', ảnh hưởng đến mức muối {diseaseProfile.Disease.SaltModifyPercent}%.");
                        }
                    }
                }
            }
            else
            {
                additionalNotes.Add("Không tìm thấy cá trong hồ.");
            }

            // Tính phần trăm muối yêu cầu
            double requiredSaltPercent = standardSalt + saltModifyPercent;
            double lowerSaltPercent = saltParameter.Parameter.WarningLowwer.HasValue
                ? saltParameter.Parameter.WarningLowwer.Value + saltModifyPercent
                : standardSalt;
            double upperSaltPercent = saltParameter.Parameter.WarningUpper.HasValue
                ? saltParameter.Parameter.WarningUpper.Value + saltModifyPercent
                : standardSalt;

            // Tính khoảng muối tối ưu
            double lowerSaltWeightKg = Math.Round(currentVolume * (lowerSaltPercent / 100), 2);
            double upperSaltWeightKg = Math.Round(currentVolume * (upperSaltPercent / 100), 2);
            double targetSaltWeightKg = Math.Round((lowerSaltWeightKg + upperSaltWeightKg) / 2, 2);

            // Khởi tạo biến
            double saltDifference = Math.Round(targetSaltWeightKg - currentSaltWeightKg, 2);
            double additionalSaltNeeded = 0.0;
            double excessSalt = 0.0;
            double waterToReplace = 0.0;

            // Tính nồng độ muối cần thêm (%)
            double additionalSaltConcentrationPercent = currentVolume > 0 ? Math.Round((additionalSaltNeeded / currentVolume) * 100, 2) : 0;

            // Kiểm tra lượng muối hiện tại có trong khoảng tối ưu không
            if (currentSaltWeightKg >= lowerSaltWeightKg && currentSaltWeightKg <= upperSaltWeightKg)
            {
                additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) đã nằm trong khoảng tối ưu ({lowerSaltWeightKg:F2} kg - {upperSaltWeightKg:F2} kg).");
            }
            else if (currentSaltWeightKg < lowerSaltWeightKg)
            {
                additionalSaltNeeded = saltDifference;
                additionalSaltConcentrationPercent = currentVolume > 0 ? Math.Round((additionalSaltNeeded / currentVolume) * 100, 2) : 0;
                additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) thấp hơn mức tối ưu ({lowerSaltWeightKg:F2} kg - {upperSaltWeightKg:F2} kg).");
                additionalNotes.Add($"Cần thêm: {additionalSaltNeeded:F2} kg muối (nồng độ hiện tại: {currentSaltConcentrationPercent:F2}%, nồng độ cần thêm: {additionalSaltConcentrationPercent:F2}%) để đạt mức mục tiêu.");
            }
            else
            {
                excessSalt = Math.Abs(saltDifference);
                double saltToRemove = currentSaltWeightKg - upperSaltWeightKg;
                if (currentSaltConcentrationKgPerL > 0)
                {
                    waterToReplace = Math.Round(saltToRemove / currentSaltConcentrationKgPerL, 2);
                    if (waterToReplace > currentVolume)
                    {
                        additionalNotes.Add($"Lỗi: Lượng nước cần thay ({waterToReplace:F2} lít) vượt quá lượng nước trong hồ ({currentVolume:F2} lít).");
                        waterToReplace = currentVolume;
                    }
                    additionalNotes.Add($"Lượng muối hiện tại ({currentSaltWeightKg:F2} kg) cao hơn mức tối ưu ({lowerSaltWeightKg:F2} kg - {upperSaltWeightKg:F2} kg).");
                    additionalNotes.Add($"Cần thay { waterToReplace:F2} lít nước (rút ra và thêm nước lọc) để đưa muối về mức tối ưu.");
                }
                else
                {
                    additionalNotes.Add("Không thể tính lượng nước cần thay vì lượng muối hiện tại bằng 0.");
                }
            }

            // Gợi ý nhắc nhở thêm muối
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
                        Title = "Nhắc nhở thêm muối",
                        Description = $"Thêm {saltPerAddition:F2} kg muối (Bước {i + 1}/{numberOfAdditions}). Tổng: {additionalSaltNeeded:F2} kg.",
                        MaintainDate = maintainDate.ToUniversalTime()
                    });
                }
            }

            // Chuẩn bị phản hồi
            var response = new CalculateSaltResponse
            {
                PondId = request.PondId,
                TotalSalt = targetSaltWeightKg,
                CurrentSalt = currentSaltWeightKg,
                SaltNeeded = additionalSaltNeeded,
                ExcessSalt = excessSalt,
                WaterNeeded = waterToReplace,
                AdditionalInstruction = additionalNotes,
                OptimalSaltFrom = lowerSaltWeightKg,
                OptimalSaltTo = upperSaltWeightKg
            };

            _saltCalculationCache[request.PondId] = response;
            return response;
        }

        /*private async Task<(double additionalWaterNeeded, List<string> messages)> CalculateWaterAdjustmentAndThresholds(
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
        }*/

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


         public async Task<SaltUpdateResponse> UpdateSaltAmount(Guid pondId, double addedSaltKg, CancellationToken cancellationToken)
             {
                 try
                 {
                     var pond = await _pondRepository.GetQueryable(p => p.PondID == pondId)
                         .FirstOrDefaultAsync(cancellationToken);

                     if (pond == null)
                     {
                         return new SaltUpdateResponse
                         {
                             Success = false,
                             Message = "Không tìm thấy hồ."
                         };
                     }

                     // Kiểm tra số lần đã thêm muối trong ngày hôm nay
                     var today = DateTime.UtcNow.Date;
                     var tomorrow = today.AddDays(1);

                     // Lấy số lượng bản ghi thông số muối từ hôm nay
                     var saltUpdatesCountToday = await _pondParamRepository
                         .GetQueryable(p => p.PondId == pondId &&
                                       p.Parameter.Name.ToLower() == "salt" &&
                                       p.CalculatedDate >= today &&
                                       p.CalculatedDate < tomorrow)
                         .Include(p => p.Parameter)
                         .CountAsync(cancellationToken);

                     // Giới hạn chỉ được thêm muối 2 lần mỗi ngày
                     if (saltUpdatesCountToday >= 2)
                     {
                         return new SaltUpdateResponse
                         {
                             Success = false,
                             Message = "Bạn không được nhập quá 2 lần trong một ngày."
                         };
                     }

                     // Lấy thông tin standardSaltParam để tạo bản ghi mới
                     var standardSaltParam = await _pondStandardParamRepository
                         .GetQueryable(p => p.Name.ToLower() == "salt")
                         .FirstOrDefaultAsync(cancellationToken);

                     if (standardSaltParam == null)
                     {
                         return new SaltUpdateResponse
                         {
                             Success = false,
                             Message = "Không tìm thấy thông số muối tiêu chuẩn."
                         };
                     }

                     // Lấy giá trị muối hiện tại từ database (bản ghi gần nhất)
                     double currentSalt = 0;
                     var latestSaltRecord = await _pondParamRepository
                         .GetQueryable(p => p.PondId == pondId && p.Parameter.Name.ToLower() == "salt")
                         .Include(p => p.Parameter)
                         .OrderByDescending(p => p.CalculatedDate)
                         .FirstOrDefaultAsync(cancellationToken);

                     if (latestSaltRecord != null)
                     {
                         currentSalt = latestSaltRecord.Value;
                     }

                     // Cộng dồn lượng muối mới và gán lại cho addedSaltKg
                     addedSaltKg = currentSalt + addedSaltKg;

                     // Tạo bản ghi mới cho giá trị muối
                     var newSaltParameter = new RelPondParameter
                     {
                         RelPondParameterId = Guid.NewGuid(),
                         PondId = pondId,
                         ParameterID = standardSaltParam.ParameterID,
                         Value = (float)addedSaltKg, // Dùng addedSaltKg nhưng đã được cộng dồn
                         CalculatedDate = DateTime.UtcNow,
                         ParameterHistoryId = Guid.NewGuid()
                     };
                     _pondParamRepository.Insert(newSaltParameter);

                     await _unitOfWork.SaveChangesAsync(cancellationToken);

                     // Cập nhật cache với giá trị mới nhất
                     if (_saltCalculationCache.TryGetValue(pondId, out CalculateSaltResponse cachedResponse))
                     {
                         cachedResponse.CurrentSalt = addedSaltKg; // Cập nhật giá trị hiện tại trong cache
                         cachedResponse.SaltNeeded = Math.Max(0, cachedResponse.TotalSalt - addedSaltKg);
                         _saltCalculationCache[pondId] = cachedResponse;
                     }

                     return new SaltUpdateResponse
                     {
                         Success = true,
                         Message = $"Đã thêm thành công {addedSaltKg} % nồng độ muối."
                     };
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"Lỗi khi cập nhật lượng muối: {ex.Message}");
                     return new SaltUpdateResponse
                     {
                         Success = false,
                         Message = $"Đã xảy ra lỗi: {ex.Message}"
                     };
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

            int numberOfAdditions = additionalSaltNeeded <= 5 ? 2 : 3;
            double saltPerAddition = additionalSaltNeeded / numberOfAdditions;

            double currentVolume = saltResponse.WaterNeeded > 0 ? saltResponse.WaterNeeded : pond.MaxVolume;
            double additionalSaltConcentrationPercent = currentVolume > 0 ? Math.Round((saltPerAddition / currentVolume) * 100, 2) : 0;

            List<SaltReminderRequest> reminders = new List<SaltReminderRequest>();

            
            DateTime startTime = DateTime.UtcNow.AddMinutes(60*9);

            for (int i = 0; i < numberOfAdditions; i++)
            {
                DateTime maintainDate = startTime.AddMinutes((cycleHours * i)*60);

                reminders.Add(new SaltReminderRequest
                {
                    PondId = pondId,
                    Title = "Thông báo thêm muối",
                    Description = $"Thêm {saltPerAddition:F2} kg muối (nồng độ: {additionalSaltConcentrationPercent:F2}%) (Lần {i + 1}/{numberOfAdditions}). Tổng cộng: {additionalSaltNeeded:F2} kg.",
                    MaintainDate = maintainDate
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
                await _unitOfWork.SaveChangesAsync(); // Commit deletions

                // Lưu reminders mới
                foreach (var reminderRequest in request.Reminders)
                {
                    // Chuyển đổi từ LocalTime sang UTC trước khi lưu
                    DateTime maintainDateUtc = reminderRequest.MaintainDate.ToUniversalTime();

                    var reminder = new PondReminder
                    {
                        PondReminderId = Guid.NewGuid(),
                        PondId = request.PondId,
                        ReminderType = ReminderType.Pond,
                        Title = reminderRequest.Title,
                        Description = reminderRequest.Description,
                        MaintainDate = maintainDateUtc, // Lưu dưới dạng UTC
                        SeenDate = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
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
