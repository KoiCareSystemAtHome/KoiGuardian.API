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

        [HttpPost("create-order/{shopId}")]
        public async Task<IActionResult> CreateOrder([FromBody] GHNRequest ghnRequest, string shopId)
        {
            try
            {
                var result = await _ghnService.CreateShippingOrder(ghnRequest, shopId);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("tracking-order")]
        public async Task<IActionResult> TrackingOrder([FromBody] TrackingGHNRequest order_code)
        {
            try
            {
                var result = await _ghnService.TrackingShippingOrder(order_code);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("get-province")]
        public async Task<IActionResult> GetProvince()
        {
            try
            {
                var result = await _ghnService.getProvince();

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("get-district")]
        public async Task<IActionResult> GetDistrict(getDistrict province_id)
        {
            try
            {
                var result = await _ghnService.getDistrict(province_id);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("get-ward")]
        public async Task<IActionResult> GetWard(getWard district_id)
        {
            try
            {
                var result = await _ghnService.getWard(district_id);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("calculate-fee/{shopId}")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] GHNShippingFeeReuqest feeRequest, string shopId)
        {
            try
            {
                var result = await _ghnService.CalculateShippingFee(feeRequest, shopId);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("cancel-order/{shopId}")]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest cancelOrderRequest, string shopId)
        {
            try
            {
                var result = await _ghnService.CancelOrder(cancelOrderRequest, shopId);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-shop")]
        public async Task<IActionResult> CreateShop([FromBody] GHNShopRequest shopRequest)
        {
            try
            {
                var result = await _ghnService.CreateGHNShop(shopRequest);

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);

                string prettyJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return Content(prettyJson, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}