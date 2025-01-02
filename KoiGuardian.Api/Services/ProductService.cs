using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Services
{
    public interface IProductService
    {
        Task<ProductResponse> CreateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken);
        Task<ProductResponse> UpdateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken);
        Task<ProductResponse> GetProductByIdAsync(string productId, CancellationToken cancellationToken);
    }

    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Shop> _shopRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public ProductService(
            IRepository<Product> productRepository,
            IRepository<Shop> shopRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _productRepository = productRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductResponse> CreateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

            // Check if the product already exists
            var existingProduct = await _productRepository.GetAsync(x => x.ProductId == productRequest.ProductId, cancellationToken);
            if (existingProduct != null)
            {
                productResponse.Status = "409";
                productResponse.Message = "Product with the given ID already exists.";
                return productResponse;
            }

            // Verify that the specified shop exists
            var shop = await _shopRepository.GetAsync(x => x.ShopId == productRequest.ShopId, cancellationToken);
            if (shop == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Specified shop does not exist.";
                return productResponse;
            }

            var product = new Product
            {
                ProductId = new Guid(),
                ProductName = productRequest.ProductName,
                Description = productRequest.Description,
                Price = productRequest.Price,
                StockQuantity = productRequest.StockQuantity,
                CategoryId = productRequest.CategoryId,
                Brand = productRequest.Brand,
                ManufactureDate = productRequest.ManufactureDate,
                ExpiryDate = productRequest.ExpiryDate,
                ParameterImpactment = productRequest.ParameterImpactment,
                ShopId = productRequest.ShopId
            };

            _productRepository.Insert(product);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                productResponse.Status = "201";
                productResponse.Message = "Product created successfully.";
            }
            catch (Exception ex)
            {
                productResponse.Status = "500";
                productResponse.Message = "Error creating product: " + ex.Message;
            }

            return productResponse;
        }

        public async Task<ProductResponse> UpdateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

            var existingProduct = await _productRepository.GetAsync(x => x.ProductId == productRequest.ProductId, cancellationToken);
            if (existingProduct == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Product with the given ID was not found.";
                return productResponse;
            }

            // Verify that the specified shop exists if shop is being changed
            if (existingProduct.ShopId != productRequest.ShopId)
            {
                var shop = await _shopRepository.GetAsync(x => x.ShopId == productRequest.ShopId, cancellationToken);
                if (shop == null)
                {
                    productResponse.Status = "404";
                    productResponse.Message = "Specified shop does not exist.";
                    return productResponse;
                }
            }

            existingProduct.ProductName = productRequest.ProductName;
            existingProduct.Description = productRequest.Description;
            existingProduct.Price = productRequest.Price;
            existingProduct.StockQuantity = productRequest.StockQuantity;
            existingProduct.CategoryId = productRequest.CategoryId;
            existingProduct.Brand = productRequest.Brand;
            existingProduct.ManufactureDate = productRequest.ManufactureDate;
            existingProduct.ExpiryDate = productRequest.ExpiryDate;
            existingProduct.ShopId = productRequest.ShopId;

            _productRepository.Update(existingProduct);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                productResponse.Status = "200";
                productResponse.Message = "Product updated successfully.";
            }
            catch (Exception ex)
            {
                productResponse.Status = "500";
                productResponse.Message = "Error updating product: " + ex.Message;
            }

            return productResponse;
        }

        public async Task<ProductResponse> GetProductByIdAsync(string productId, CancellationToken cancellationToken)
        {
            var productGuid = Guid.Parse(productId); // or Guid.TryParse for safer conversion
            var product = await _productRepository.GetAsync(x => x.ProductId == productGuid, cancellationToken);
            if (product == null)
            {
                return new ProductResponse
                {
                    Status = "404",
                    Message = "Product not found."
                };
            }
            return new ProductResponse
            {
                Status = "200",
                Message = "Product retrieved successfully."
            };
        }
    }
}
