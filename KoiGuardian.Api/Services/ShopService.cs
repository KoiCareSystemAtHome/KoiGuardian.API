using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;

namespace KoiGuardian.Api.Services
{
    public interface IShopService
    {
        Task<ShopResponse> CreateShop(ShopRequest shopRequest, CancellationToken cancellation);
        
        Task<ShopResponse> GetShop(string shopId, CancellationToken cancellation);
        Task<ShopResponse> DeleteShop(string shopId, CancellationToken cancellation);
    }

    public class ShopService : IShopService
    {
        private readonly IRepository<Shop> _shopRepository;

        public ShopService(IRepository<Shop> shopRepository)
        {
            _shopRepository = shopRepository;
        }

        public async Task<ShopResponse> CreateShop(ShopRequest shopRequest, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();
            var shop = await _shopRepository.GetAsync(x => x.ShopId.Equals(shopRequest.ShopId), cancellation);

            if (shop is null)
            {
                shop = new()
                {
                    ShopId = shopRequest.ShopId,
                    ShopName = shopRequest.ShopName,
                    ShopRate = shopRequest.ShopRate,
                    ShopDescription = shopRequest.ShopDescription,
                    ShopAddress = shopRequest.ShopAddress,
                    IsActivate = true,
                    BizLicences = shopRequest.BizLicences
                };
                _shopRepository.Insert(shop);
                shopResponse.Status = "201";
                shopResponse.Message = "Create Shop Success";
            }
            else
            {
                shopResponse.Status = "409";
                shopResponse.Message = "Shop Already Exists";
            }
            return shopResponse;
        }



        public async Task<ShopResponse> GetShop(string shopId, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();
            var shop = await _shopRepository.GetAsync(x => x.ShopId.Equals(shopId), cancellation);
            if (shop is not null)
            {
                shopResponse.Shop = new ShopDTO
                {
                    ShopId = shop.ShopId,
                    ShopName = shop.ShopName,
                    ShopRate = shop.ShopRate,
                    ShopDescription = shop.ShopDescription,
                    ShopAddress = shop.ShopAddress,
                    IsActivate = shop.IsActivate,
                    BizLicences = shop.BizLicences
                };
                shopResponse.Status = "200";
                shopResponse.Message = "Get Shop Success";
            }
            else
            {
                shopResponse.Status = "404";
                shopResponse.Message = "Shop Not Found";
            }
            return shopResponse;
        }

        public async Task<ShopResponse> DeleteShop(string shopId, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();
            var shop = await _shopRepository.GetAsync(x => x.ShopId.Equals(shopId), cancellation);

            if (shop is not null)
            {
                shop.IsActivate = false;
                _shopRepository.Delete(shop);
                shopResponse.Status = "200";
                shopResponse.Message = "Delete Shop Success";
            }
            else
            {
                shopResponse.Status = "404";
                shopResponse.Message = "Shop Not Found";
            }
            return shopResponse;
        }
    }
}