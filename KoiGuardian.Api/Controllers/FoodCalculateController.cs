using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodCalculateController
        ( FoodCalculatorService _services
        ) : ControllerBase
    {
        [HttpGet]
        public async Task<CalculateFoodResponse> Cal([FromBody] CalculateFoodRequest createBlog)
        {
            return await _services.Calculate(createBlog);
        }
    }
}
