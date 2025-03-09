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

        [HttpGet("getByShopId")]
        public async Task<List<OrderDetailResponse>> GetByShopId(Guid orderId)
        {
            return await service.GetOrdersByShopIdAsync(orderId);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var response = await service.CreateOrderAsync(request);

            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderRequest request)
        {
            var response = await service.UpdateOrderAsync(request);

            return Ok(response);
        }

        [HttpPut("updateOrderCode")]
        public async Task<IActionResult> UpdateOrderCode([FromBody] UpdateOrderCodeRequest request)
        {
            var response = await service.UpdateOrderCodeAsync(request);

            return Ok(response);
        }

        [HttpPut("updateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var response = await service.UpdateOrderStatusAsync(request);

            return Ok(response);
        }
    }
}
