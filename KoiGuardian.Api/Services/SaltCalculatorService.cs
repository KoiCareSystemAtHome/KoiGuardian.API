using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Azure.Core;
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

        Task<List<Notification>> GetSaltNotifications(Guid pondId);


        Task<bool> AdjustSaltAdditionStartTime(Guid pondId, DateTime newStartTime);

    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;
        private readonly IRepository<PondStandardParam> _pondStandardParamRepository;
        private readonly IRepository<KoiDiseaseProfile> _koiDiseaseProfileRepository;
        private readonly IRepository<Notification> _notificationRepository;
        private static readonly ConcurrentDictionary<Guid, CalculateSaltResponse> _saltCalculationCache = new();





        public SaltCalculatorService(
            IRepository<Pond> pondRepository,
            IRepository<KoiDiseaseProfile> koiDiseaseProfile,
            IRepository<Notification> notificationRepository,
            IRepository<RelPondParameter> pondParamRepository)
            
        {
            _pondRepository = pondRepository;
            _pondParamRepository = pondParamRepository;
            _notificationRepository = notificationRepository;
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
                .Include(p => p.Fish);
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
                .Include(p => p.Parameter);
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
                .Include(p => p.Parameter);
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

                // Create and save notifications every 7 hours
                await CreateSaltNotifications(pond, additionalSaltNeeded);
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

            _saltCalculationCache[request.PondId] = response;
            return response;
        }

        private async Task CreateSaltNotifications(Pond pond, double additionalSaltNeeded)
        {
            var now = DateTime.UtcNow; 
            const int hoursInterval = 7; 

            // Calculate number of reminders based on salt addition steps
            double maxSaltIncreasePerTime = pond.MaxVolume * 0.0005; // 0.05% of MaxVolume in kg
            int numberOfReminders = (int)Math.Ceiling(additionalSaltNeeded / maxSaltIncreasePerTime);

            for (int i = 0; i < numberOfReminders; i++)
            {
                var sendDate = now.AddHours(hoursInterval * i);

                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    ReceiverId = Guid.Parse(pond.OwnerId), // Assuming OwnerId is string; adjust if Guid
                    Type = "SaltAdditionReminder",
                    Title = "Nhắc nhở thêm muối cho hồ",
                    Content = $"Hồ của bạn cần thêm {additionalSaltNeeded:F2} kg muối. Lần {i + 1}: Thêm {(additionalSaltNeeded / numberOfReminders):F2} kg muối vào {sendDate:HH:mm dd/MM/yyyy}.",
                    Seendate = sendDate,
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(new { PondId = pond.PondID, SaltNeeded = additionalSaltNeeded, Step = i + 1, SaltPerStep = additionalSaltNeeded / numberOfReminders })
                };

                 _notificationRepository.Insert(notification); // Assuming Insert returns Task
            }
        }

        public async Task<SaltAdditionProcessResponse> GetSaltAdditionProcess(Guid pondId)
        {
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

            double maxSaltIncreasePerTime = pond.MaxVolume * 0.0005;
            int numberOfAdditions = (int)Math.Ceiling(additionalSaltNeeded / maxSaltIncreasePerTime);
            double saltPerAddition = additionalSaltNeeded / numberOfAdditions;

            instructions.Add($"Tổng lượng muối cần thêm: {additionalSaltNeeded:F2} kg.");
            instructions.Add($"Số lần thêm muối: {numberOfAdditions}.");
            instructions.Add($"Lượng muối mỗi lần: {saltPerAddition:F2} kg.");
            instructions.Add($"Quy tắc 1: Tăng tối đa 0.05% ({maxSaltIncreasePerTime:F2} kg) mỗi lần.");
            instructions.Add("Quy tắc 2: Chờ 7 giờ giữa các lần thêm muối để đảm bảo an toàn cho cá.");
           

            return new SaltAdditionProcessResponse
            {
                PondId = pondId,
                Instructions = instructions
            };
        }

        public async Task<List<Notification>> GetSaltNotifications(Guid pondId)
        {
            var notificationsQuery = _notificationRepository.GetQueryable(n =>
                n.Type == "SaltAdditionReminder" &&
                n.Data.Contains(pondId.ToString())) // Filter by PondId in Data (JSON string)
                .OrderBy(n => n.Seendate); // Sort by scheduled time

            return await notificationsQuery.ToListAsync();
        }

        public async Task<bool> AdjustSaltAdditionStartTime(Guid pondId, DateTime newStartTime)
        {
            // Kiểm tra hồ cá có tồn tại không
            var pond = await _pondRepository.GetQueryable(p => p.PondID == pondId).FirstOrDefaultAsync();
            if (pond == null)
            {
                return false;
            }

            // Lấy tất cả thông báo thêm muối hiện tại cho hồ
            var existingNotifications = await GetSaltNotifications(pondId);
            if (!existingNotifications.Any())
            {
                return false;
            }

            // Xóa thông báo cũ - sửa lỗi ở đây
            foreach (var notification in existingNotifications)
            {
                
                 _notificationRepository.Delete(notification);
            }

            // Lấy thông tin từ notification đầu tiên để tái tạo
            var firstNotification = existingNotifications.First();
            var notificationData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(firstNotification.Data);

            double additionalSaltNeeded = (double)notificationData.SaltNeeded;
            int totalSteps = existingNotifications.Count;
            double saltPerStep = additionalSaltNeeded / totalSteps;

            // Tạo lại thông báo với thời gian mới
            const int hoursInterval = 7;
            for (int i = 0; i < totalSteps; i++)
            {
                var sendDate = newStartTime.AddHours(hoursInterval * i);

                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    ReceiverId = Guid.Parse(pond.OwnerId),
                    Type = "SaltAdditionReminder",
                    Title = "Nhắc nhở thêm muối cho hồ",
                    Content = $"Hồ của bạn cần thêm {additionalSaltNeeded:F2} kg muối. Lần {i + 1}: Thêm {saltPerStep:F2} kg muối vào {sendDate:HH:mm dd/MM/yyyy}.",
                    Seendate = sendDate,
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        PondId = pond.PondID,
                        SaltNeeded = additionalSaltNeeded,
                        Step = i + 1,
                        SaltPerStep = saltPerStep
                    })
                };

                 _notificationRepository.Insert(notification);
            }

            return true;
        }




    }
}