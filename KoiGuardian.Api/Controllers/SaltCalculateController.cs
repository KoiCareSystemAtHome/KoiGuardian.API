using Microsoft.AspNetCore.Mvc;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Api.Services;
using System;
using KoiGuardian.DataAccess.Db;

namespace KoiGuardian.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaltCalculateController : ControllerBase
    {
        private readonly ISaltCalculatorService _saltCalculatorService;

        public SaltCalculateController(ISaltCalculatorService saltCalculatorService)
        {
            _saltCalculatorService = saltCalculatorService;
        }
        [HttpPost("calculate")]
        public async Task<CalculateSaltResponse> CalculateSalt([FromBody] CalculateSaltRequest request)
        {
            return await _saltCalculatorService.CalculateSalt(request);
        }
        [HttpPost("addition-process")]
        public async Task<SaltAdditionProcessResponse> GetSaltAdditionProcess([FromQuery] Guid pondId)
        {
            return await _saltCalculatorService.GetSaltAdditionProcess(pondId);
        }

        [HttpPost("notifications")]
        public async Task<List<Notification>> GetSaltNotifications([FromQuery] Guid pondId)
        {
            return await _saltCalculatorService.GetSaltNotifications(pondId);
        }
        [HttpPost("adjust-start-time")]
        public async Task<bool> AdjustSaltAdditionStartTime([FromBody] AdjustSaltStartTimeRequest request)
        {
            return await _saltCalculatorService.AdjustSaltAdditionStartTime(
                request.PondId,
                request.NewStartTime);
        }
        [HttpPost("create-notifications")]
        public async Task<IActionResult> CreateSaltNotifications([FromBody] NotificationRequest request)
        {
            try
            {
                // Validate the request
                if (request == null || request.PondId == Guid.Empty)
                {
                    return BadRequest(new { Message = "Invalid request: PondId is required" });
                }

                if (request.StartTime < DateTime.UtcNow)
                {
                    return BadRequest(new { Message = "Start time cannot be in the past" });
                }

                // Call the service to create notifications
                bool success = await _saltCalculatorService.CreateSaltNotificationsForUser(request);

                if (!success)
                {
                    return BadRequest(new
                    {
                        Message = "Failed to create notifications. Ensure salt calculation exists and additional salt is needed."
                    });
                }

                // Get the newly created notifications
                var notifications = await _saltCalculatorService.GetSaltNotifications(request.PondId);

                return Ok(new
                {
                    Success = true,
                    Message = "Salt addition notifications created successfully",
                    Notifications = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"An error occurred while creating notifications: {ex.Message}"
                });
            }
        }

   




    }
}
