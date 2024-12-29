using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
                return Ok(result);  // Return the response from GHN API
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);  // Return error message if the request fails
            }
        }
    }
}
