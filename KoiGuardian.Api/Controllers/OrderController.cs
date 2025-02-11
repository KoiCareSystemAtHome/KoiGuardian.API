using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(
        IOrderService service) : ControllerBase
    {
        [HttpGet]
        public async Task<List<OrderFilterResponse>> Filter([FromQuery]OrderFilterRequest request)
        {
            return await service.FilterOrder(request);
        }


        [HttpGet("detail")]
        public async Task<OrderDetailResponse> OrderDetail( Guid orderId)
        {
            return await service.GetDetail(orderId);
        }
    }
}
