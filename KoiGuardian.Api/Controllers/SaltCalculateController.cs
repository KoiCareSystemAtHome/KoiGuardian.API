using Microsoft.AspNetCore.Mvc;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task<IActionResult> CalculateSalt([FromBody] CalculateSaltRequest request)
        {
            try
            {
                if (request == null || request.PondId == Guid.Empty)
                {
                    return BadRequest(new { Message = "Invalid request: PondId is required" });
                }

                var response = await _saltCalculatorService.CalculateSalt(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"An error occurred while calculating salt: {ex.Message}"
                });
            }
        }

        [HttpPost("addition-process")]
        public async Task<IActionResult> GetSaltAdditionProcess([FromQuery] Guid pondId)
        {
            try
            {
                if (pondId == Guid.Empty)
                {
                    return BadRequest(new { Message = "Invalid request: PondId is required" });
                }

                var response = await _saltCalculatorService.GetSaltAdditionProcess(pondId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"An error occurred while getting salt addition process: {ex.Message}"
                });
            }
        }

        [HttpGet("reminders")]
        public async Task<IActionResult> GetSaltReminders([FromQuery] Guid pondId)
        {
            try
            {
                if (pondId == Guid.Empty)
                {
                    return BadRequest(new { Message = "Invalid request: PondId is required" });
                }

                var reminders = await _saltCalculatorService.GetSaltReminders(pondId);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"An error occurred while retrieving reminders: {ex.Message}"
                });
            }
        }

       

        [HttpPost("generate-salt-reminders")]
        public async Task<IActionResult> GenerateSaltAdditionReminders([FromBody] GenerateSaltRemindersRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || request.PondId == Guid.Empty)
                {
                    return BadRequest(new { Message = "Yêu cầu không hợp lệ: PondId là bắt buộc" });
                }

                // Kiểm tra chu kỳ phải là 12 hoặc 24 giờ
                if (request.CycleHours != 12 && request.CycleHours != 24)
                {
                    return BadRequest(new { Message = "Chu kỳ chỉ có thể là 12 hoặc 24 giờ" });
                }

                var reminders = await _saltCalculatorService.GenerateSaltAdditionRemindersAsync(
                    request.PondId,
                    request.CycleHours,
                    cancellationToken);

                if (reminders == null || !reminders.Any())
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "Không cần thêm muối hoặc không tạo được reminder.",
                        Reminders = new List<SaltReminderRequest>()
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Đã tạo thành công các reminder thêm muối",
                    Reminders = reminders
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Đã xảy ra lỗi khi tạo reminders: {ex.Message}"
                });
            }
        }

        [HttpPost("update-salt-amount")]
        public async Task<IActionResult> UpdateSaltAmount([FromBody] UpdateSaltAmountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || request.PondId == Guid.Empty)
                {
                    return BadRequest(new { Success = false, Message = "Yêu cầu không hợp lệ: PondId là bắt buộc" });
                }

                if (request.AddedSaltKg < 0)
                {
                    return BadRequest(new { Success = false, Message = "Lượng muối không thể là số âm" });
                }

                var result = await _saltCalculatorService.UpdateSaltAmount(request.PondId, request.AddedSaltKg, cancellationToken);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.Message // Sử dụng thông báo từ service
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = result.Message // Sử dụng thông báo thành công từ service
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Đã xảy ra lỗi khi cập nhật lượng muối: {ex.Message}"
                });
            }
        }

        [HttpPost("save-reminders")]
        public async Task<IActionResult> SaveSelectedReminders([FromBody] SaveSaltRemindersRequest request)
        {
            try
            {
                if (request == null || request.PondId == Guid.Empty)
                {
                    return BadRequest(new { Message = "Invalid request: PondId is required" });
                }

                if (request.Reminders == null || !request.Reminders.Any())
                {
                    return BadRequest(new { Message = "No reminders selected to save" });
                }

                bool success = await _saltCalculatorService.SaveSelectedSaltReminders(request);
                if (!success)
                {
                    return BadRequest(new
                    {
                        Message = "Failed to save reminders. Ensure the pond exists and salt calculation is valid."
                    });
                }

                var savedReminders = await _saltCalculatorService.GetSaltReminders(request.PondId);
                return Ok(new
                {
                    Success = true,
                    Message = "Selected salt reminders saved successfully",
                    Reminders = savedReminders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"An error occurred while saving reminders: {ex.Message}"
                });
            }
        }
        [HttpPut("reminders/update-maintain-date")]
        public async Task<IActionResult> UpdateMaintainDate([FromBody] UpdateSaltReminderRequest request)
        {
            var success = await _saltCalculatorService.UpdateSaltReminderDateAsync(request);

            if (!success)
            {
                return BadRequest("Yêu cầu không hợp lệ hoặc không tìm thấy reminder.");
            }

            return Ok("Cập nhật thời gian bảo trì thành công.");
        }

    }
}