using Azure.Core;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace KoiGuardian.Api.Services
{
    public interface IShopService
    {
        //Task<ShopResponse> CreateShop(ShopRequest shopRequest, CancellationToken cancellation);
        Task<ShopResponse> GetShopById(Guid shopId, CancellationToken cancellation);
        Task<ShopResponse> DeleteShop(Guid shopId, CancellationToken cancellation);
        Task<ShopResponse> GetShopByIdAsync(Guid shopId, CancellationToken cancellationToken);

        Task<ShopResponse> UpdateShop(Guid shopId, ShopRequest shopRequest, CancellationToken cancellation);

        Task<IList<Shop>> GetAllShopAsync(CancellationToken cancellationToken);

        Task<ShopResponse> GetShopByUserId(Guid userId, CancellationToken cancellation);
    }

    public class ShopService : IShopService
    {
        private readonly IRepository<Shop> _shopRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ShopService(
            IRepository<Shop> shopRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

       /* public async Task<ShopResponse> CreateShop(ShopRequest shopRequest, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();

            // Remove this line as it overwrites the input parameter
            // shopRequest = new ShopRequest();

            var shop = await _shopRepository.GetAsync(x => x.ShopId.Equals(shopRequest.ShopId), cancellation);

            if (shop is null)
            {
                shop = new()
                {
                    
                    ShopName = shopRequest.ShopName,
                    ShopRate = shopRequest.ShopRate,
                    ShopDescription = shopRequest.ShopDescription,
                    ShopAddress = shopRequest.ShopAddress,
                    IsActivate = true,
                    BizLicences = shopRequest.BizLicences
                    // Products will be empty by default - no need to initialize
                };

                _shopRepository.Insert(shop);

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellation);
                    shopResponse.Status = "201";
                    shopResponse.Message = "Create Shop Success";
                   
                }
                catch (Exception ex)
                {
                    shopResponse.Status = "500";
                    shopResponse.Message = "Error creating shop: " + ex.Message;
                }
            }
            else
            {
                shopResponse.Status = "409";
                shopResponse.Message = "Shop Already Exists";
            }

            return shopResponse;
        }*/

        public async Task<ShopResponse> UpdateShop(Guid shopId, ShopRequest shopRequest, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();
            var shop = await _shopRepository
                .GetAsync(x => x.ShopId == shopId, cancellation);

            string addressNote = JsonSerializer.Serialize(new
            {
                ProvinceName = shopRequest.ShopAddress.ProvinceName,
                ProvinceId = shopRequest.ShopAddress.ProvinceId,
                DistrictName = shopRequest.ShopAddress.DistrictName,
                DistrictId = shopRequest.ShopAddress.DistrictId,
                WardName = shopRequest.ShopAddress.WardName,
                WardId = shopRequest.ShopAddress.WardId
            });

            if (shop is not null)
            {
                // Chỉ cập nhật thông tin cơ bản của shop
                shop.ShopName = shopRequest.ShopName;
                shop.ShopRate = shopRequest.ShopRate;
                shop.ShopDescription = shopRequest.ShopDescription;
                shop.ShopAddress = addressNote;
                shop.IsActivate = shopRequest.IsActivate;
                shop.GHNId = shopRequest.GhnId;
                shop.BizLicences = shopRequest.BizLicences;

                _shopRepository.Update(shop);

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellation);
                    shopResponse.Status = "200";
                    shopResponse.Message = "Update Shop Success";
                    shopResponse.Shop = new ShopRequestDetails
                    {
                        ShopId = shop.ShopId,
                        ShopName = shop.ShopName,
                        ShopRate = shop.ShopRate,
                        ShopDescription = shop.ShopDescription,
                        ShopAvatar = shop.ShopAvatar ?? "",
                        ShopAddress = new AddressDto
                        {
                            DistrictName = shopRequest.ShopAddress.ProvinceName,
                            DistrictId = shopRequest.ShopAddress.ProvinceId,
                            ProvinceName = shopRequest.ShopAddress.DistrictName,
                            ProvinceId = shopRequest.ShopAddress.DistrictId,
                            WardName = shopRequest.ShopAddress.WardName,
                            WardId = shopRequest.ShopAddress.WardId,
                        },
                        IsActivate = shop.IsActivate,
                        BizLicences = shop.BizLicences
                    };
                }
                catch (Exception ex)
                {
                    shopResponse.Status = "500";
                    shopResponse.Message = "Error updating shop: " + ex.Message;
                }
            }
            else
            {
                shopResponse.Status = "404";
                shopResponse.Message = "Shop Not Found";
            }

            return shopResponse;
        }


        public async Task<ShopResponse> GetShopById(Guid shopId, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();
            var shop = await _shopRepository
                .GetQueryable()
                .Include(s => s.Products)  // Include products
                .FirstOrDefaultAsync(x => x.ShopId == shopId, cancellation);


            if (shop is not null)
            {
                var address = JsonSerializer.Deserialize<AddressDto>(shop.ShopAddress);

                shopResponse.Shop = new ShopRequestDetails
                {
                    ShopId = shop.ShopId,
                    ShopName = shop.ShopName,
                    ShopRate = shop.ShopRate,
                    ShopDescription = shop.ShopDescription,
                    ShopAddress = new AddressDto
                    {
                        DistrictName = address.DistrictName,
                        DistrictId = address.DistrictId,
                        ProvinceName = address.ProvinceName,
                        ProvinceId = address.ProvinceId,
                        WardName = address.WardName,
                        WardId = address.WardId,
                    },
                    IsActivate = shop.IsActivate,
                    BizLicences = shop.BizLicences,
                    Products = shop.Products?.Select(p => new ProductDetailsRequest
                    {
                       
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Image = p.Image,
                        Brand = p.Brand,
                        Weight = p.Weight,
                        StockQuantity = p.StockQuantity,
                        Description = p.Description,
                        CategoryId = p.CategoryId,

                    }).ToList()
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

        public async Task<ShopResponse> DeleteShop(Guid shopId, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();
            var shop = await _shopRepository.GetAsync(x => x.ShopId.Equals(shopId), cancellation);

            if (shop is not null)
            {
                shop.IsActivate = false;
                _shopRepository.Delete(shop);

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellation);
                    shopResponse.Status = "200";
                    shopResponse.Message = "Delete Shop Success";
                }
                catch (Exception ex)
                {
                    shopResponse.Status = "500";
                    shopResponse.Message = "Error deleting shop: " + ex.Message;
                }
            }
            else
            {
                shopResponse.Status = "404";
                shopResponse.Message = "Shop Not Found";
            }

            return shopResponse;
        }

        public async Task<ShopResponse> GetShopByIdAsync(Guid shopId, CancellationToken cancellationToken)
        {
            var shopResponse = new ShopResponse();

            var shop = await _shopRepository
                .GetQueryable()
                .Include(s => s.Products)
                .FirstOrDefaultAsync(x => x.ShopId == shopId, cancellationToken);

            if (shop is not null)
            {
                AddressDto addressDto;
                try
                {
                    addressDto = !string.IsNullOrEmpty(shop.ShopAddress)
                        ? JsonSerializer.Deserialize<AddressDto>(shop.ShopAddress)
                        : new AddressDto { ProvinceName = "No address info" };
                }
                catch (JsonException)
                {
                    addressDto = new AddressDto { ProvinceName = "Invalid address" };
                }

                shopResponse.Shop = new ShopRequestDetails
                {
                    ShopId = shop.ShopId,
                    ShopName = shop.ShopName,
                    ShopRate = shop.ShopRate,
                    ShopDescription = shop.ShopDescription,
                    ShopAddress = addressDto,
                    IsActivate = shop.IsActivate,
                    BizLicences = shop.BizLicences,
                    Products = shop.Products?.Select(p => new ProductDetailsRequest
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Image = p.Image,
                        Brand = p.Brand,
                        Weight = p.Weight,
                        StockQuantity = p.StockQuantity,
                        Description = p.Description,
                        CategoryId = p.CategoryId
                    }).ToList()
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
    

        public async Task<IList<Shop>> GetAllShopAsync(CancellationToken cancellationToken)
        {
            return await _shopRepository.GetQueryable().ToListAsync(cancellationToken);
        }

        public async Task<ShopResponse> GetShopByUserId(Guid userId, CancellationToken cancellation)
        {
            var shopResponse = new ShopResponse();

            string userIdString = userId.ToString();

            var shop = await _shopRepository
                .GetQueryable()
                .Include(s => s.Products)
                .FirstOrDefaultAsync(x => x.UserId == userIdString, cancellation);

            if (shop is not null)
            {
                AddressDto addressDto;
                try
                {
                    addressDto = !string.IsNullOrEmpty(shop.ShopAddress)
                        ? JsonSerializer.Deserialize<AddressDto>(shop.ShopAddress)
                        : new AddressDto { ProvinceName = "No address info" };
                }
                catch (JsonException)
                {
                    addressDto = new AddressDto { ProvinceName = "Invalid address" };
                }

                shopResponse.Shop = new ShopRequestDetails
                {
                    ShopId = shop.ShopId,
                    ShopName = shop.ShopName,
                    ShopRate = shop.ShopRate,
                    ShopDescription = shop.ShopDescription,
                    ShopAddress = addressDto,
                    IsActivate = shop.IsActivate,
                    BizLicences = shop.BizLicences,
                    Products = shop.Products?.Select(p => new ProductDetailsRequest
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Image = p.Image,
                        Brand = p.Brand,
                        Weight = p.Weight,
                        StockQuantity = p.StockQuantity,
                        Description = p.Description,
                        CategoryId = p.CategoryId
                    }).ToList()
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
    }
}
