using Microsoft.AspNetCore.Mvc;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using KoiGuardian.Api.Services;
using System;
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
            if (request == null || request.PondId == Guid.Empty)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid request parameters."
                });
            }

            try
            {
                CalculateSaltResponse response = await _saltCalculatorService.CalculateSalt(request);
                return Ok(new
                {
                    Success = true,
                    PondId = response.PondId,
                    TotalSalt = response.TotalSalt,
                    Message = "Tính toán thành công.",
                    AdditionalInstructions = response.AdditionalInstruction
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình xử lý.",
                    Details = ex.Message
                });
            }
        }
    }
}
