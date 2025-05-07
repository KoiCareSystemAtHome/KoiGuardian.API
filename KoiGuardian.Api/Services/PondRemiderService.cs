using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Services
{
    public interface IPondReminderService
    {
        Task<List<PondRemiderResponse>> GetRemindersByPondIdAsync(Guid pondId, CancellationToken cancellationToken);
        Task<string> UpdateByidAsync(Guid id, CancellationToken cancellationToken);
        Task<List<PondRemiderResponse>> GetRemindersByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken);
        Task<PondRemiderResponse> GetRemindersByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<PondReminder?> GenerateMaintenanceReminderAsync(Guid pondId, CancellationToken cancellationToken);
        Task<List<PondReminder>> GenerateRecurringMaintenanceRemindersAsync(Guid pondId, DateTime endDate, int cycleDays, CancellationToken cancellationToken);
        Task SaveMaintenanceReminderAsync(PondReminder reminder, CancellationToken cancellationToken);
    }

    public class PondReminderService : IPondReminderService
    {
        private readonly IRepository<PondReminder> _reminderRepository;
        private readonly IRepository<RelPondParameter> _relPondParameter;
        private readonly IRepository<PondStandardParam> _parameterRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public PondReminderService(
            IRepository<PondReminder> reminderRepository,
            IRepository<RelPondParameter> relPondParameter,
            IRepository<PondStandardParam> parameterRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _reminderRepository = reminderRepository;
            _relPondParameter = relPondParameter;
            _parameterRepository = parameterRepository;
            _unitOfWork = unitOfWork;
        }

        // Sinh ra lịch bảo trì cho hồ (hàm chính gộp tất cả logic)
        public async Task<PondReminder> GenerateMaintenanceReminderAsync(Guid pondId, CancellationToken cancellationToken)
        {
            const int LookbackMonths = 3;
            const int DaysSinceLastUpdateThreshold = 14; // Ngưỡng 14 ngày chưa cập nhật
            var cutoffDate = DateTime.UtcNow.AddMonths(-LookbackMonths);
            DateTime? earliestMaintenanceDate = null;
            string? earliestDescription = null;
            float earliestValue = 0;
            string? earliestParamName = null;

            // Load tất cả dữ liệu của hồ trong 3 tháng gần nhất
            var pondParameters = await _relPondParameter.FindAsync(
                rp => rp.PondId == pondId && rp.CalculatedDate >= cutoffDate,
                cancellationToken: cancellationToken
            );

            // Nếu không có dữ liệu
            if (!pondParameters.Any())
            {
                return new PondReminder
                {
                    PondReminderId = Guid.NewGuid(),
                    PondId = pondId,
                    ReminderType = ReminderType.Maintenance,
                    Title = "Lịch bảo trì hồ",
                    Description = "Không có đủ dữ liệu thích hợp. Yêu cầu bảo trì.",
                    MaintainDate = DateTime.UtcNow.AddDays(1).AddMinutes(60 * 7).ToUniversalTime(), // Ngày hôm sau
                    SeenDate = DateTime.MinValue.ToUniversalTime()
                };
            }

            // Nhóm theo ParameterID
            var parameterGroups = pondParameters
                .GroupBy(rp => rp.ParameterID)
                .ToDictionary(g => g.Key, g => g.OrderBy(rp => rp.CalculatedDate).ToList());

            // Load master data parameters
            var parameterIds = parameterGroups.Keys.ToList();
            var parameters = await _parameterRepository.FindAsync(
                p => parameterIds.Contains(p.ParameterID),
                cancellationToken: cancellationToken
            );
            var parameterLookup = parameters.ToDictionary(p => p.ParameterID);

            // Kiểm tra lần cập nhật cuối cùng
            var latestUpdate = pondParameters.Max(rp => rp.CalculatedDate);
            var daysSinceLastUpdate = (DateTime.UtcNow - latestUpdate).Days;

            if (daysSinceLastUpdate > DaysSinceLastUpdateThreshold)
            {
                return new PondReminder
                {
                    PondReminderId = Guid.NewGuid(),
                    PondId = pondId,
                    ReminderType = ReminderType.Maintenance,
                    Title = "Bảo trì cho hồ",
                    Description = $"Quá lâu chưa cập nhật hồ (lần cập nhật cuối: {daysSinceLastUpdate} ngày trước).",
                    MaintainDate = DateTime.UtcNow.AddDays(1).AddMinutes(60 * 7).ToUniversalTime(), // Ngày hôm sau
                    SeenDate = DateTime.MinValue.ToUniversalTime()
                };
            }

            // Tính toán ngày bảo trì cho từng parameter
            foreach (var paramId in parameterGroups.Keys)
            {
                if (!parameterLookup.TryGetValue(paramId, out var parameter))
                    continue;

                var paramData = parameterGroups[paramId];
                if (paramData.Count < 2) // Cần ít nhất 2 bản ghi để tính gia tăng
                    continue;

                var currentValue = paramData.Last().Value;
                var maxSafeDensity = parameter.DangerUpper;
                var minSafeDensity = parameter.DangerLower;
                var warningUpper = parameter.WarningUpper;
                var warningLower = parameter.WarningLowwer;

                // Bỏ qua nếu không có ngưỡng nào được định nghĩa
                if (!maxSafeDensity.HasValue && !minSafeDensity.HasValue &&
                    !warningUpper.HasValue && !warningLower.HasValue)
                    continue;

                DateTime maintenanceDate;
                string description;

                // Kiểm tra ngưỡng nguy hiểm (quá cao)
                if (maxSafeDensity.HasValue && currentValue >= maxSafeDensity.Value)
                {
                    maintenanceDate = DateTime.UtcNow.AddDays(1); // Ngày hôm sau
                    description = $"{parameter.Name} chạm ngưỡng nguy hiểm cao ({currentValue}/{maxSafeDensity.Value})";
                }
                // Kiểm tra ngưỡng nguy hiểm (quá thấp)
                else if (minSafeDensity.HasValue && currentValue <= minSafeDensity.Value)
                {
                    maintenanceDate = DateTime.UtcNow.AddDays(1); // Ngày hôm sau
                    description = $"{parameter.Name} chạm ngưỡng nguy hiểm thấp ({currentValue}/{minSafeDensity.Value})";
                }
                // Kiểm tra ngưỡng cảnh báo (quá cao)
                else if (warningUpper.HasValue && currentValue > warningUpper.Value)
                {
                    maintenanceDate = DateTime.UtcNow.AddDays(1); // Ngày hôm sau
                    description = $"{parameter.Name} vượt ngưỡng cảnh báo cao ({currentValue}/{warningUpper.Value})";
                }
                // Kiểm tra ngưỡng cảnh báo (quá thấp)
                else if (warningLower.HasValue && currentValue < warningLower.Value)
                {
                    maintenanceDate = DateTime.UtcNow.AddDays(1); // Ngày hôm sau
                    description = $"{parameter.Name} dưới ngưỡng cảnh báo thấp ({currentValue}/{warningLower.Value})";
                }
                // Tính ngày bảo trì dựa trên gia tăng hoặc giảm
                else
                {
                    double totalChange = 0;
                    int count = 0;
                    for (int i = 1; i < paramData.Count; i++)
                    {
                        double change = paramData[i].Value - paramData[i - 1].Value;
                        totalChange += change;
                        count++;
                    }
                    double avgChange = count > 0 ? totalChange / count : 0;

                    if (avgChange > 0 && maxSafeDensity.HasValue)
                    {
                        int daysUntilMaintenance = (int)Math.Ceiling((maxSafeDensity.Value - currentValue) / avgChange);
                        if (daysUntilMaintenance < 0)
                            continue;
                        maintenanceDate = DateTime.UtcNow.AddDays(daysUntilMaintenance);
                        description = $"{parameter.Name} đang tiến gần đến giới hạn không an toàn cao ({currentValue}/{maxSafeDensity.Value})";
                    }
                    else if (avgChange < 0 && minSafeDensity.HasValue)
                    {
                        int daysUntilMaintenance = (int)Math.Ceiling((currentValue - minSafeDensity.Value) / -avgChange);
                        if (daysUntilMaintenance < 0)
                            continue;
                        maintenanceDate = DateTime.UtcNow.AddDays(daysUntilMaintenance);
                        description = $"{parameter.Name} đang tiến gần đến giới hạn không an toàn thấp ({currentValue}/{minSafeDensity.Value})";
                    }
                    else
                    {
                        continue; // Không có thay đổi đáng kể hoặc không có ngưỡng để so sánh
                    }
                }

                if (!earliestMaintenanceDate.HasValue || maintenanceDate < earliestMaintenanceDate)
                {
                    earliestMaintenanceDate = maintenanceDate;
                    earliestDescription = description;
                    earliestValue = currentValue;
                    earliestParamName = parameter.Name;
                }
            }

            // Nếu không tính được ngày cụ thể từ các parameter
            if (!earliestMaintenanceDate.HasValue)
            {
                earliestMaintenanceDate = DateTime.UtcNow.AddDays(1); // Ngày hôm sau nếu không có dữ liệu cụ thể
                earliestDescription = "Đã lên lịch bảo trì (Hồ mới hoặc không đủ thông số)";
                earliestValue = 0;
                earliestParamName = "Pond";
            }

            return new PondReminder
            {
                PondReminderId = Guid.NewGuid(),
                PondId = pondId,
                ReminderType = ReminderType.Maintenance,
                Title = $"Lịch Bảo Dưỡng Hồ Cho {earliestParamName}",
                Description = $"Nồng Độ Hiện tại: {earliestValue} - {earliestDescription}. Cần Được Bảo Trì.",
                MaintainDate = earliestMaintenanceDate.Value.AddMinutes(60 * 7).ToUniversalTime(),
                SeenDate = DateTime.MinValue.ToUniversalTime()
            };
        }
        // Lấy thông tin nhắc nhở theo ID
        public async Task<PondRemiderResponse> GetRemindersByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var pondReminder = await _reminderRepository.GetAsync(
                predicate: rp => rp.PondReminderId == id,
                include: query => query.Include(p => p.Pond),
                orderBy: query => query.OrderBy(p => p.Pond.Name),
                cancellationToken: cancellationToken);

            if (pondReminder == null)
            {
                throw new InvalidOperationException($"Reminder with ID '{id}' not found.");
            }

            return new PondRemiderResponse
            {
                PondReminderId = pondReminder.PondReminderId,
                PondId = pondReminder.PondId,
                PondName = pondReminder.Pond.Name,
                ReminderType = pondReminder.ReminderType.ToString(),
                Title = pondReminder.Title,
                Description = pondReminder.Description,
                MaintainDate = pondReminder.MaintainDate,
                SeenDate = pondReminder.SeenDate,
            };
        }

        // Lấy danh sách nhắc nhở theo PondId
        public async Task<List<PondRemiderResponse>> GetRemindersByPondIdAsync(Guid pondId, CancellationToken cancellationToken)
        {
            var pondReminders = await _reminderRepository.FindAsync(
                predicate: rp => rp.PondId == pondId,
                include: query => query.Include(p => p.Pond),
                orderBy: query => query.OrderBy(p => p.Pond.Name),
                cancellationToken: cancellationToken);

            return pondReminders.Select(pondReminder => new PondRemiderResponse
            {
                PondReminderId = pondReminder.PondReminderId,
                PondId = pondReminder.PondId,
                PondName = pondReminder.Pond.Name,
                ReminderType = pondReminder.ReminderType.ToString(),
                Title = pondReminder.Title,
                Description = pondReminder.Description,
                MaintainDate = pondReminder.MaintainDate,
                SeenDate = pondReminder.SeenDate,
            }).ToList();
        }

        // Tạo lịch bảo trì định kỳ theo các khoảng thời gian
        public async Task<List<PondReminder>> GenerateRecurringMaintenanceRemindersAsync(Guid pondId, DateTime endDate, int cycleDays, CancellationToken cancellationToken)
        {
            if (cycleDays <= 0)
            {
                throw new ArgumentException("Số ngày chu kỳ phải lớn hơn 0.");
            }

            List<PondReminder> reminders = new List<PondReminder>();
            // Lấy giờ từ endDate
            TimeSpan timeOfDay = endDate.TimeOfDay;
            // Bắt đầu từ ngày mai, với giờ lấy từ endDate
            DateTime startDate = DateTime.UtcNow.AddDays(1).AddHours(7).Date + timeOfDay;

            while (startDate <= endDate)
            {
                reminders.Add(new PondReminder
                {
                    PondReminderId = Guid.NewGuid(),
                    PondId = pondId,
                    ReminderType = ReminderType.RecurringMaintenance,
                    Title = "Bảo Dưỡng Định Kì",
                    Description = "Thời gian bảo dưỡng định kì cho hồ.",
                    MaintainDate = startDate.ToUniversalTime(),
                    SeenDate = DateTime.MinValue.ToUniversalTime()
                });

                startDate = startDate.AddDays(cycleDays);
            }

            foreach (var reminder in reminders)
            {
                await SaveMaintenanceReminderAsync(reminder, cancellationToken);
            }

            return reminders;
        }

        // Lưu lịch bảo trì vào DB
        public async Task SaveMaintenanceReminderAsync(PondReminder reminder, CancellationToken cancellationToken)
        {
            try
            {
                reminder.SeenDate = reminder.SeenDate.ToUniversalTime();
                reminder.MaintainDate = reminder.MaintainDate.ToUniversalTime();
                _reminderRepository.Insert(reminder);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save maintenance reminder: {ex.Message}");
            }
        }

        public async Task<List<PondRemiderResponse>> GetRemindersByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken)
        {
            var pondReminders = await _reminderRepository.FindAsync(
                predicate: rp => rp.Pond.OwnerId.Equals(ownerId.ToString()),
                include: query => query.Include(p => p.Pond),
                orderBy: query => query.OrderBy(p => p.Pond.Name),
                cancellationToken: cancellationToken);

            return pondReminders.Select(pondReminder => new PondRemiderResponse
            {
                PondReminderId = pondReminder.PondReminderId,
                PondName = pondReminder.Pond.Name,
                PondId = pondReminder.PondId,
                ReminderType = pondReminder.ReminderType.ToString(),
                Title = pondReminder.Title,
                Description = pondReminder.Description,
                MaintainDate = pondReminder.MaintainDate,
                SeenDate = pondReminder.SeenDate,
            }).ToList();
        }

        public async Task<string> UpdateByidAsync(Guid id, CancellationToken cancellationToken)
        {
            var pondReminder = await _reminderRepository.GetAsync(
                 rp => rp.PondReminderId == id,
                 cancellationToken: cancellationToken);

            if (pondReminder == null)
            {
                throw new InvalidOperationException($"Reminder with ID '{id}' not found.");
                return "Fail";
            }
            pondReminder.SeenDate = DateTime.UtcNow;
            _reminderRepository.Update(pondReminder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return "success";
        }
    }
}