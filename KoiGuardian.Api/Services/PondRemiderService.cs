using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface IPondReminderService
    {
        Task<List<PondRemiderResponse>> GetRemindersByPondIdAsync(Guid pondId, CancellationToken cancellationToken);

        Task<PondRemiderResponse> GetRemindersByidAsync (Guid id, CancellationToken cancellationToken);
        Task<double> CalculateAverageParameterIncreaseAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken);
        Task<DateTime> CalculateMaintenanceDateAsync(Guid pondId, CancellationToken cancellationToken);
        Task<PondReminder?> GenerateMaintenanceReminderAsync(Guid pondId, CancellationToken cancellationToken);
        //bảo trì địng kì
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

        // Tính toán sự thay đổi trung bình của tham số trong 3 tháng qua
        public async Task<double> CalculateAverageParameterIncreaseAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken)
        {
            const int LookbackMonths = 3; // Thời gian tính trung bình là 3 tháng
            var cutoffDate = DateTime.UtcNow.AddMonths(-LookbackMonths);

            // FindAsync with dynamic parameterId
            var parameterData = await _relPondParameter.FindAsync(
                rp => rp.PondId == pondId && rp.CalculatedDate >= cutoffDate
                     && rp.ParameterHistoryId == parameterId,
                cancellationToken: cancellationToken
            );

            if (!parameterData.Any())
            {
                throw new InvalidOperationException($"No data found for parameter ID '{parameterId}' in the last 3 months.");
            }

            // Tính sự gia tăng trung bình của tham số trong khoảng thời gian này
            double totalIncrease = 0;
            int count = 0;

            for (int i = 1; i < parameterData.Count; i++)
            {
                double increase = parameterData[i].Value - parameterData[i - 1].Value;
                totalIncrease += increase;
                count++;
            }

            return count > 0 ? totalIncrease / count : 0;
        }

        // Tính ngày bảo trì dựa trên sự gia tăng tham số trung bình
        public async Task<DateTime> CalculateMaintenanceDateAsync(Guid pondId, CancellationToken cancellationToken)
        {
            // Lấy tất cả các thông số hiện tại của hồ từ RelPondParameter
            var pondParameters = await _relPondParameter.FindAsync(
                rp => rp.PondId == pondId,
                cancellationToken: cancellationToken
            );

            if (!pondParameters.Any())
            {
                throw new InvalidOperationException($"No parameter data found for pond ID '{pondId}'.");
            }

            DateTime earliestMaintenanceDate = DateTime.MaxValue;

            // Duyệt qua từng thông số của hồ
            foreach (var pondParam in pondParameters)
            {
                // Lấy thông tin chuẩn của thông số từ bảng master data
                var parameter = await _parameterRepository.GetAsync(
                    rp => rp.ParameterID.Equals(pondParam.ParameterID),
                    cancellationToken: cancellationToken
                );

                if (parameter == null)
                {
                    continue; // Bỏ qua nếu không tìm thấy thông số chuẩn
                }

                // Xử lý ngưỡng nguy hiểm và cảnh báo nếu là null
                double? maxSafeDensity = parameter.DangerUpper; // Ngưỡng nguy hiểm
                double? warningUpper = parameter.WarningUpper;  // Ngưỡng cảnh báo cao
                double currentDensity = pondParam.Value;        // Giá trị hiện tại của thông số

                // Nếu warningUpper không null, kiểm tra và gửi cảnh báo
                if (warningUpper.HasValue && currentDensity > warningUpper.Value)
                {
                    Console.WriteLine($"Warning: Parameter '{parameter.Name}' (ID: {parameter.ParameterID}) " +
                                      $"is out of safe range. Current value: {currentDensity}, Warning Upper: {warningUpper.Value}");
                }

                // Nếu maxSafeDensity là null, coi như không có ngưỡng nguy hiểm -> bỏ qua bảo trì dựa trên thông số này
                if (!maxSafeDensity.HasValue)
                {
                    continue; // Không cần tính ngày bảo trì vì ngưỡng là "rất cao"
                }

                // Nếu đã vượt ngưỡng nguy hiểm
                if (currentDensity >= maxSafeDensity.Value)
                {
                    return DateTime.Now; // Bảo trì ngay lập tức
                }

                // Tính tốc độ gia tăng trung bình của thông số
                double averageParameterIncrease = await CalculateAverageParameterIncreaseAsync(
                    pondId,
                    pondParam.ParameterID,
                    cancellationToken
                );

                // Nếu không có gia tăng (hoặc giảm), bỏ qua thông số này
                if (averageParameterIncrease <= 0)
                {
                    continue;
                }

                // Tính số ngày để đạt ngưỡng nguy hiểm
                int daysUntilMaintenance = (int)Math.Ceiling((maxSafeDensity.Value - currentDensity) / averageParameterIncrease);
                DateTime maintenanceDate = DateTime.Now.AddDays(daysUntilMaintenance);

                // Cập nhật ngày bảo trì sớm nhất
                if (maintenanceDate < earliestMaintenanceDate)
                {
                    earliestMaintenanceDate = maintenanceDate;
                }
            }

            // Nếu không có ngày nào được tính toán (ví dụ: tất cả thông số đều ổn định hoặc ngưỡng là null), trả về ngày xa trong tương lai
            if (earliestMaintenanceDate == DateTime.MaxValue)
            {
                return DateTime.Now.AddMonths(6); // Giả định mặc định là 6 tháng nếu không có vấn đề
            }

            return earliestMaintenanceDate;
        }

        // Sinh ra lịch bảo trì cho hồ
        public async Task<PondReminder?> GenerateMaintenanceReminderAsync(Guid pondId, CancellationToken cancellationToken)
        {
            try
            {
                var maintenanceDate = await CalculateMaintenanceDateAsync(pondId, cancellationToken);

                // Chuyển đổi sang UTC
                maintenanceDate = maintenanceDate.ToUniversalTime();

                return new PondReminder
                {
                    PondReminderId = Guid.NewGuid(),
                    PondId = pondId,
                    ReminderType = ReminderType.Pond,
                    Title = "Maintenance",
                    Description = "One or more parameters are reaching unsafe limits. Maintenance required.",
                    MaintainDate = maintenanceDate,
                    SeenDate = DateTime.MinValue.ToUniversalTime()
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate maintenance reminder: {ex.Message}");
            }
        }

        public async Task<PondRemiderResponse> GetRemindersByidAsync(Guid id, CancellationToken cancellationToken)
        {
            var pondReminder = await _reminderRepository.GetAsync(
            rp => rp.PondReminderId == id, cancellationToken: cancellationToken);

            PondRemiderResponse pondRemiderResponse = new PondRemiderResponse
            {
                PondReminderId = pondReminder.PondReminderId,
                PondId = pondReminder.PondId,
                ReminderType = pondReminder.ReminderType.ToString(),
                Title = pondReminder.Title,
                Description = pondReminder.Description,
                MaintainDate = pondReminder.MaintainDate,
                SeenDate = pondReminder.SeenDate,
            };
            return pondRemiderResponse;
        }

        public async Task<List<PondRemiderResponse>> GetRemindersByPondIdAsync(Guid pondId, CancellationToken cancellationToken)
        {
            var pondReminders = await _reminderRepository.FindAsync(
        rp => rp.PondId == pondId, cancellationToken: cancellationToken);

            return pondReminders.Select(pondReminder => new PondRemiderResponse
            {
                PondReminderId = pondReminder.PondReminderId,
                PondId = pondReminder.PondId,
                ReminderType = pondReminder.ReminderType.ToString(),
                Title = pondReminder.Title,
                Description = pondReminder.Description,
                MaintainDate = pondReminder.MaintainDate,
                SeenDate = pondReminder.SeenDate,
            }).ToList();
        }

        //Tạo LỊch Bảo trì đinh kì theo các khoảng time
        public async Task<List<PondReminder>> GenerateRecurringMaintenanceRemindersAsync(Guid pondId, DateTime endDate, int cycleDays, CancellationToken cancellationToken)
        {
            if (cycleDays <= 0)
            {
                throw new ArgumentException("Số ngày chu kỳ phải lớn hơn 0.");
            }

            List<PondReminder> reminders = new List<PondReminder>();
            DateTime startDate = DateTime.UtcNow;

            while (startDate <= endDate)
            {
                startDate = startDate.AddDays(cycleDays);
                reminders.Add(new PondReminder
                {
                    PondReminderId = Guid.NewGuid(),
                    PondId = pondId,
                    ReminderType = ReminderType.Pond,
                    Title = "Scheduled Maintenance",
                    Description = "Routine maintenance scheduled for the pond.",
                    MaintainDate = startDate,
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
                _reminderRepository.Insert(reminder);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save maintenance reminder: {ex.Message}");
            }
        }
    }
}
