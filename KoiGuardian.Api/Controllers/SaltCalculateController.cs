using Microsoft.AspNetCore.Mvc;
using KoiGuardian.Models.Request;
using KoiGuardian.Api.Services;

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
        public IActionResult CalculateSalt([FromBody] CalculateSaltRequest request)
        {
            try
            {
                double totalSalt = _saltCalculatorService.CalculateSalt(request);
                return Ok(new
                {
                    Success = true,
                    TotalSalt = totalSalt,
                    Message = "Tính toán thành công."
                });
            }
            catch (ArgumentOutOfRangeException ex)
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
                    Message = "Đã xảy ra lỗi trong quá trình xử lý.",
                    Details = ex.Message
                });
            }
        }
    }
}