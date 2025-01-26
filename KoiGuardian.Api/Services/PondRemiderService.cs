using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;

namespace KoiGuardian.Api.Services
{
    public interface IPondRemiderService
    {
        Task<DateTime> CalculateMaintenanceDateAsync(double currentNitrogenDensity, double averageNitrogenIncreasePerDay);

        Task<PondReminder> GenerateMaintenanceReminderAsync(Guid pondId, double currentNitrogenDensity, double averageNitrogenIncreasePerDay);

        Task SaveMaintenanceReminderAsync(PondReminder reminder);
    }

    public class PondRemiderService : IPondRemiderService
    {
        private readonly IRepository<PondReminder> _remiderREpository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public async Task<DateTime> CalculateMaintenanceDateAsync(double currentNitrogenDensity, double averageNitrogenIncreasePerDay)
        {
            const double MaxSafeNitrogenDensity = 50.0; // Ngưỡng an toàn nitơ

            if (currentNitrogenDensity >= MaxSafeNitrogenDensity)
            {
                throw new ArgumentException("Nitrogen density is already above the safe limit.");
            }

            // Số ngày để đạt ngưỡng an toàn
            int daysUntilMaintenance = (int)Math.Ceiling((MaxSafeNitrogenDensity - currentNitrogenDensity) / averageNitrogenIncreasePerDay);

            // Trả về ngày bảo trì
            return await Task.FromResult(DateTime.Now.AddDays(daysUntilMaintenance));
        }

        public async Task<PondReminder> GenerateMaintenanceReminderAsync(Guid pondId, double currentNitrogenDensity, double averageNitrogenIncreasePerDay)
        {
            var maintenanceDate = await CalculateMaintenanceDateAsync(currentNitrogenDensity, averageNitrogenIncreasePerDay);

            return await Task.FromResult(new PondReminder
            {
                PondReminderId = Guid.NewGuid(),
                PondId = pondId,
                ReminderType = ReminderType.Pond,
                Title = "Nitrogen Maintenance",
                Description = "Nitrogen level is reaching unsafe limits. Maintenance required.",
                MaintainDate = maintenanceDate,
                SeenDate = DateTime.MinValue
            });
        }

        public Task SaveMaintenanceReminderAsync(PondReminder reminder)
        {
            throw new NotImplementedException();
        }
    }
}
