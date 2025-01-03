using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Services
{
    public interface IProductService
    {
        Task<ProductResponse> CreateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken);
        Task<ProductResponse> UpdateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken);
        Task<Product> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken);

        Task<IEnumerable<Product>> SearchProductsAsync(string productName, string brand, string parameterImpact, CancellationToken cancellationToken);
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
                ShopId = productRequest.ShopId
            };


            product.SetParameterImpacts(productRequest.ParameterImpacts);

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

        public async Task<Product> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken)
        {

            return await _productRepository
            .GetQueryable()
            .FirstOrDefaultAsync(b => b.ProductId == productId, cancellationToken);
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(
        string productName,
        string brand,
        string parameterImpact,
        CancellationToken cancellationToken)
        {
            var query = _productRepository.GetQueryable();

            // Apply filters if provided
            if (!string.IsNullOrWhiteSpace(productName))
            {
                query = query.Where(p => p.ProductName.Contains(productName));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(p => p.Brand.Contains(brand));
            }

            if (!string.IsNullOrWhiteSpace(parameterImpact))
            {
                
                query = query.Where(p => p.ParameterImpactment.Contains(parameterImpact));
            }

            // Execute query and return results
            return await query
                .AsNoTracking()  // For better performance since we're just reading
                .ToListAsync(cancellationToken);
        }
    }
}

