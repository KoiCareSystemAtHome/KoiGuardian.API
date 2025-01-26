using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PondReminderController : ControllerBase
    {
        private readonly IPondRemiderService _pondReminderService;

        public PondReminderController(IPondRemiderService pondReminderService)
        {
            _pondReminderService = pondReminderService;
        }

        // Tính toán và trả về lịch bảo trì (không lưu)
        [HttpPost("calculate-maintenance")]
        public async Task<IActionResult> CalculateMaintenance([FromBody] MaintenanceRequest request)
        {
            try
            {
                var reminder = await _pondReminderService.GenerateMaintenanceReminderAsync(
                    request.PondId,
                    request.CurrentNitrogenDensity,
                    request.AverageNitrogenIncreasePerDay
                );

                return Ok(reminder);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Error calculating maintenance reminder.",
                    Error = ex.Message
                });
            }
        }

        // Lưu lịch bảo trì vào DB
        [HttpPost("save-maintenance")]
        public async Task<IActionResult> SaveMaintenance([FromBody] PondReminder reminder)
        {
            try
            {
                await _pondReminderService.SaveMaintenanceReminderAsync(reminder);

                return Ok(new
                {
                    Message = "Maintenance reminder saved successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Error saving maintenance reminder.",
                    Error = ex.Message
                });
            }
        }
    }
}
