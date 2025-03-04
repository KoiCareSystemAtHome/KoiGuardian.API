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
        ( IFoodCalculatorService _services
        ) : ControllerBase
    {
        [HttpGet]
        public async Task<CalculateFoodResponse> Cal([FromQuery] CalculateFoodRequest createBlog)
        {
            return await _services.Calculate(createBlog);
        }
    }
}
