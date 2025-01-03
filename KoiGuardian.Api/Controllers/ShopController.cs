using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopController(IShopService shopService)
        {
            _shopService = shopService;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateShop([FromBody] ShopRequest shopRequest, CancellationToken cancellationToken)
        {
            var response = await _shopService.CreateShop(shopRequest, cancellationToken);

            if (response.Status == "201")
                return Created("", response);

            if (response.Status == "409")
                return Conflict(response);

            return BadRequest(response);
        }

        [HttpGet]
        [Route("{shopId}")]
        public async Task<IActionResult> GetShopById(Guid shopId, CancellationToken cancellationToken)
        {
            var response = await _shopService.GetShop(shopId, cancellationToken);

            if (response.Status == "200")
                return Ok(response);

            if (response.Status == "404")
                return NotFound(response);

            return BadRequest(response);
        }

        [HttpGet("all-shop")]
        public async Task<IList<Shop>> GetAllCategoriesAsync(CancellationToken cancellationToken)
        {
            return await _shopService.GetAllShopAsync(cancellationToken);
        }

        [HttpDelete]
        [Route("{shopId}")]
        public async Task<IActionResult> DeleteShop(Guid shopId, CancellationToken cancellationToken)
        {
            var response = await _shopService.DeleteShop(shopId, cancellationToken);

            if (response.Status == "200")
                return Ok(response);

            if (response.Status == "404")
                return NotFound(response);

            return BadRequest(response);
        }
    }
}
