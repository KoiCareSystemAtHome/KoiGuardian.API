using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Azure.Core;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
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

        Task<bool> CreateSaltNotificationsForUser(NotificationRequest request);

        Task<bool> UpdateSaltAmount(Guid pondId, double addedSaltKg, CancellationToken cancellationToken);





    }

    public class SaltCalculatorService : ISaltCalculatorService
    {
        private readonly IRepository<Pond> _pondRepository;
        private readonly IRepository<RelPondParameter> _pondParamRepository;
        private readonly IRepository<PondStandardParam> _pondStandardParamRepository;
        private readonly IRepository<KoiDiseaseProfile> _koiDiseaseProfileRepository;
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        private static readonly ConcurrentDictionary<Guid, CalculateSaltResponse> _saltCalculationCache = new();





        public SaltCalculatorService(
            IRepository<Pond> pondRepository,
            IRepository<KoiDiseaseProfile> koiDiseaseProfile,
            IRepository<Notification> notificationRepository,
            IRepository<PondStandardParam> pondStandardParamRepository,
            IRepository<RelPondParameter> pondParamRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)

        {
            _pondRepository = pondRepository;
            _pondParamRepository = pondParamRepository;
            _notificationRepository = notificationRepository;
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

                // Tính lượng nước cần thêm để pha loãng từ currentSaltConcentration xuống targetSaltWeightKg
                double newTotalVolume = currentSaltConcentration / requiredSaltPercent;
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

            _saltCalculationCache[request.PondId] = response;
            return response;
        }

        public async Task<bool> CreateSaltNotificationsForUser(NotificationRequest request)
        {
           
            if (!_saltCalculationCache.TryGetValue(request.PondId, out CalculateSaltResponse saltResponse))
            {
                return false;
            }

            
            var pond = await _pondRepository.GetQueryable(p => p.PondID == request.PondId).FirstOrDefaultAsync();
            if (pond == null)
            {
                return false;
            }

            
            double additionalSaltNeeded = saltResponse.SaltNeeded;
            if (additionalSaltNeeded <= 0)
            {
                return false;
            }

           
            var existingNotifications = await GetSaltNotifications(request.PondId);
            foreach (var notification1 in existingNotifications)
            {
                _notificationRepository.Delete(notification1);
            }

            
            int numberOfAdditions = additionalSaltNeeded <= 0.5 ? 2 : 3;
            double saltPerAddition = additionalSaltNeeded / numberOfAdditions;
            int hoursInterval = request.HoursInterval > 0 ? Math.Clamp(request.HoursInterval, 12, 24) : 12; // Mặc định 12 giờ

            
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                ReceiverId = Guid.Parse(pond.OwnerId),
                Type = "SaltAdditionReminder",
                Title = "Nhắc nhở thêm muối cho hồ",
                Content = $"Thêm {additionalSaltNeeded:F2} kg muối, chia thành {numberOfAdditions} lần, mỗi lần {saltPerAddition:F2} kg, cách nhau {hoursInterval} giờ, bắt đầu từ {request.StartTime:HH:mm dd/MM/yyyy}.",
                Seendate = request.StartTime,
                Data = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    PondId = pond.PondID,
                    SaltNeeded = additionalSaltNeeded,
                    NumberOfAdditions = numberOfAdditions,
                    SaltPerAddition = saltPerAddition,
                    HoursInterval = hoursInterval,
                    StartTime = request.StartTime
                })
            };

            _notificationRepository.Insert(notification);
           

            return true;
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

            // Tính lượng nước thay mỗi lần
            double currentVolume = pond.MaxVolume;
            int waterChangePercent = 20; // Thay 20% mỗi lần
            double waterToReplacePerStep = currentVolume * (waterChangePercent / 100.0);

            instructions.Add($"- Sau đó, giảm dần nồng độ bằng cách thay {waterToReplacePerStep:F2} lít nước ({waterChangePercent}% hồ) mỗi lần, cách nhau 1-2 ngày, cho đến khi muối giảm về mức an toàn.");


           

            return new SaltAdditionProcessResponse { PondId = pondId, Instructions = instructions };
        }



        public async Task<List<Notification>> GetSaltNotifications(Guid pondId)
        {
            // Lấy value từ DB
            var notificationQuery = _notificationRepository.GetQueryable(n =>
                n.Type == "SaltAdditionReminder" &&
                n.Data.Contains(pondId.ToString()))
                .OrderBy(n => n.Seendate);

            var notification = await notificationQuery.FirstOrDefaultAsync();
            if (notification == null)
            {
                return new List<Notification>();
            }

            // Phân tích dữ liệu JSON
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(notification.Data);
            int numberOfAdditions = (int)data.NumberOfAdditions;
            double saltPerAddition = (double)data.SaltPerAddition;
            int hoursInterval = (int)data.HoursInterval;
            DateTime startTime = (DateTime)data.StartTime;
            double additionalSaltNeeded = (double)data.SaltNeeded;
            Guid pondIdFromData = (Guid)data.PondId;

            // Tạo danh sách thông báo ảo dựa trên thông tin
            var virtualNotifications = new List<Notification>();
            for (int i = 0; i < numberOfAdditions; i++)
            {
                DateTime sendTime = startTime.AddHours(hoursInterval * i);
                var virtualNotification = new Notification
                {
                    NotificationId = notification.NotificationId, 
                    ReceiverId = notification.ReceiverId,
                    Type = "SaltAdditionReminder",
                    Title = "Nhắc nhở thêm muối cho hồ",
                    Content = $"Lần {i + 1}/{numberOfAdditions}: Thêm {saltPerAddition:F2} kg muối vào {sendTime:HH:mm dd/MM/yyyy}. Quan sát cá sau khi thêm.",
                    Seendate = sendTime,
                    Data = notification.Data 
                };
                virtualNotifications.Add(virtualNotification);
            }

            return virtualNotifications.OrderBy(n => n.Seendate).ToList();
        }

        public async Task<bool> AdjustSaltAdditionStartTime(Guid pondId, DateTime newStartTime)
        {
            var pond = await _pondRepository.GetQueryable(p => p.PondID == pondId).FirstOrDefaultAsync();
            if (pond == null) return false;

            var existingNotifications = await GetSaltNotifications(pondId);
            if (!existingNotifications.Any()) return false;

            // Lấy dữ liệu từ thông báo đầu tiên
            var firstNotification = existingNotifications.First();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(firstNotification.Data);

            double saltNeeded = (double)data.SaltNeeded;
            int numAdditions = (int)data.NumberOfAdditions;
            double saltPerAddition = (double)data.SaltPerAddition;
            int hoursInterval = (int)data.HoursInterval;

            // Xóa thông báo cũ từ database
            var dbNotifications = await _notificationRepository.GetQueryable(n =>
                n.Type == "SaltAdditionReminder" && n.Data.Contains(pondId.ToString())).ToListAsync();
            dbNotifications.ForEach(n => _notificationRepository.Delete(n));

            
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                ReceiverId = Guid.Parse(pond.OwnerId),
                Type = "SaltAdditionReminder",
                Title = "Nhắc nhở thêm muối cho hồ",
                Content = $"Thêm {saltNeeded:F2} kg muối, chia thành {numAdditions} lần, mỗi lần {saltPerAddition:F2} kg, cách nhau {hoursInterval} giờ, bắt đầu từ {newStartTime:HH:mm dd/MM/yyyy}.",
                Seendate = newStartTime,
                Data = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    PondId = pond.PondID,
                    SaltNeeded = saltNeeded,
                    NumberOfAdditions = numAdditions,
                    SaltPerAddition = saltPerAddition,
                    HoursInterval = hoursInterval,
                    StartTime = newStartTime
                })
            };
            _notificationRepository.Insert(notification);

            return true;
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
                    saltParameter.Value = (float)addedSaltKg;
                    saltParameter.CalculatedDate = DateTime.UtcNow;
                    _pondParamRepository.Update(saltParameter);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (_saltCalculationCache.TryGetValue(pondId, out CalculateSaltResponse cachedResponse))
                {
                    cachedResponse.CurrentSalt = addedSaltKg;
                    cachedResponse.SaltNeeded = Math.Max(0, cachedResponse.TotalSalt - addedSaltKg);
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



    }
}