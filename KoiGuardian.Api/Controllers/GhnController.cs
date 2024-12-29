using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GhnController : ControllerBase
    {
        private readonly GhnService _ghnService;

        public GhnController(GhnService ghnService)
        {
            _ghnService = ghnService;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] GHNRequest ghnRequest)
        {
            try
            {
                // Call the service to create the shipping order
                var result = await _ghnService.CreateShippingOrder(ghnRequest);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                // Chuyển đổi lại chuỗi JSON với format đẹp
                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true // Kích hoạt định dạng đẹp
                });

                // Trả về JSON định dạng đẹp
                return Content(prettyJson, "application/json");// Return the response from GHN API
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);  // Return error message if the request fails
            }
        }

        [HttpPost("tracking-order")]
        public async Task<IActionResult> TrackingOrder([FromBody] TrackingGHNRequest order_code)
        {
            try
            {
                // Call the service to create the shipping order
                var result = await _ghnService.TrackingShippingOrder(order_code);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                // Chuyển đổi lại chuỗi JSON với format đẹp
                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true // Kích hoạt định dạng đẹp
                });

                // Trả về JSON định dạng đẹp
                return Content(prettyJson, "application/json");// Return the response from GHN API
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);  // Return error message if the request fails
            }
        }
    }
}
