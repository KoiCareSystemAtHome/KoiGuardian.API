using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface IPondReminderService
    {
        Task<double> CalculateAverageParameterIncreaseAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken);
        Task<DateTime> CalculateMaintenanceDateAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken);
        Task<PondReminder?> GenerateMaintenanceReminderAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken);
        Task SaveMaintenanceReminderAsync(PondReminder reminder, CancellationToken cancellationToken);
    }

    public class PondReminderService : IPondReminderService
    {
        private readonly IRepository<PondReminder> _reminderRepository;
        private readonly IRepository<RelPondParameter> _relPondParameter;
        private readonly IRepository<Parameter> _parameterRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public PondReminderService(
            IRepository<PondReminder> reminderRepository,
            IRepository<RelPondParameter> relPondParameter,
            IRepository<Parameter> parameterRepository,
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
        public async Task<DateTime> CalculateMaintenanceDateAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken)
        {
            // Lấy tham số từ database để lấy ngưỡng cảnh báo và ngưỡng tối đa
            var parameter = await _parameterRepository.GetAsync(rp => rp.HistoryId.Equals(parameterId), cancellationToken: cancellationToken);
            if (parameter == null)
            {
                throw new InvalidOperationException($"Parameter with ID '{parameterId}' not found.");
            }

            double maxSafeDensity = (double)parameter.DangerUpper; // Ngưỡng tối đa cho tham số
           // double warningLower = (double)parameter.WarningLowwer; // Ngưỡng cảnh báo thấp
            double warningUpper = (double)parameter.WarningUpper; // Ngưỡng cảnh báo cao

            // Lấy giá trị tham số hiện tại từ RelPondParameter
            var latestParameterData = await _relPondParameter.FindAsync(
                rp => rp.PondId == pondId && rp.ParameterHistoryId == parameterId,
                cancellationToken: cancellationToken
            );

            if (latestParameterData == null || !latestParameterData.Any())
            {
                throw new InvalidOperationException($"No data found for parameter ID '{parameterId}' for the pond.");
            }

            double currentDensity = latestParameterData.First().Value; // Assuming only one result here
            double averageParameterIncrease = await CalculateAverageParameterIncreaseAsync(pondId, parameterId, cancellationToken);

            // Kiểm tra xem tham số có vượt ngưỡng cảnh báo hay không
            if (/*currentDensity < warningLower ||*/ currentDensity > warningUpper)
            {
                // Gửi cảnh báo nếu tham số vượt quá ngưỡng cảnh báo
                Console.WriteLine($"Warning: The parameter '{parameterId}' density is out of the safe range. Current value: {currentDensity}");
            }

            // Kiểm tra xem tham số có vượt ngưỡng tối đa không
            if (currentDensity >= maxSafeDensity)
            {
                throw new InvalidOperationException($"The parameter '{parameterId}' density is already above the safe limit. Maintenance is required immediately.");
            }

            // Tính toán số ngày cần thiết để đạt ngưỡng an toàn cho tham số
            int daysUntilMaintenance = (int)Math.Ceiling((maxSafeDensity - currentDensity) / averageParameterIncrease);

            return DateTime.Now.AddDays(daysUntilMaintenance);
        }

        // Sinh ra lịch bảo trì cho hồ
        public async Task<PondReminder?> GenerateMaintenanceReminderAsync(Guid pondId, Guid parameterId, CancellationToken cancellationToken)
        {
            try
            {
                var maintenanceDate = await CalculateMaintenanceDateAsync(pondId, parameterId, cancellationToken);

                // Ensure that the maintenance date is in UTC
                maintenanceDate = maintenanceDate.ToUniversalTime();

                var parameter = await _parameterRepository.FindAsync(rp => rp.HistoryId == parameterId, cancellationToken: cancellationToken);
                if (parameter == null)
                {
                    throw new InvalidOperationException($"Parameter with ID '{parameterId}' not found.");
                }

                return new PondReminder
                {
                    PondReminderId = Guid.NewGuid(),
                    PondId = pondId,
                    ReminderType = ReminderType.Pond,
                    Title = "Maintenance",
                    Description = "level is reaching unsafe limits. Maintenance required.",
                    MaintainDate = maintenanceDate, // Ensure this is in UTC
                    SeenDate = DateTime.MinValue.ToUniversalTime() // Ensure SeenDate is also in UTC
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate maintenance reminder: {ex.Message}");
            }
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
