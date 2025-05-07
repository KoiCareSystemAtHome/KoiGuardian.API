using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
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

       /* [HttpPost("create")]
        public async Task<ShopResponse> CreateShop([FromBody] ShopRequest shopRequest, CancellationToken cancellationToken)
        {
            return await _shopService.CreateShop(shopRequest, cancellationToken);
        }*/

        [HttpGet("shop/{shopId}")]
        public async Task<ShopResponse> GetShopById(Guid shopId, CancellationToken cancellationToken)
        {
            return await _shopService.GetShopByIdAsync(shopId, cancellationToken);
        }

        [HttpGet]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<IList<Shop>> GetAllShops(CancellationToken cancellationToken)
        {
            return await _shopService.GetAllShopAsync(cancellationToken);
        }

        [HttpDelete("{shopId}")]
        [Authorize]
        public async Task<ShopResponse> DeleteShop(Guid shopId, CancellationToken cancellationToken)
        {
            return await _shopService.DeleteShop(shopId, cancellationToken);
        }

        [HttpPut("{shopId}")]
        [Authorize]
        public async Task<ShopResponse> UpdateShop(Guid shopId, [FromBody] ShopRequest shopRequest, CancellationToken cancellationToken)
        {
            return await _shopService.UpdateShop(shopId, shopRequest, cancellationToken);
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ShopResponse> GetShopByUserId(Guid userId, CancellationToken cancellationToken)
        {
            return await _shopService.GetShopByUserId(userId, cancellationToken);
        }

        [HttpGet("get-shop-with-balance")]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<List<ShopBalanceResponseDto>> GetAllShopWithBalances(CancellationToken cancellationToken)
        {
            return await _shopService.GetAllShopsWithBalancesAsync(cancellationToken);
        }
    }
}