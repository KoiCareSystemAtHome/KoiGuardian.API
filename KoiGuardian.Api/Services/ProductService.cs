﻿using Azure.Core;
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
        Task<ProductResponse> CreateProductAsync(string baseUrl,ProductRequest productRequest, CancellationToken cancellationToken);
        Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productRequest, CancellationToken cancellationToken);
        Task<ProductDetailResponse> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken);

        Task<IEnumerable<ProductSearchResponse>> SearchProductsAsync(
      string productName,
      string brand,
      string parameterImpact,
      string categoryName,
      CancellationToken cancellationToken);

        Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken cancellationToken);
       
    }

    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Shop> _shopRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        private readonly IImageUploadService _imageUploadService;

        public ProductService(
            IRepository<Product> productRepository,
            IRepository<Shop> shopRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork,
            IImageUploadService imageUpload
            )
            
        {
            _productRepository = productRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
            _imageUploadService = imageUpload;
        }

        public async Task<ProductResponse> CreateProductAsync(string baseUrl, ProductRequest productRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

          

            // Verify that the specified shop exists
            var shop = await _shopRepository.GetAsync(x => x.ShopId == productRequest.ShopId, cancellationToken);
            if (shop == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Specified shop does not exist.";
                return productResponse;
            }

            // Create the new product
            var product = new Product
            {
                
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

            // Upload the image
            var image = await _imageUploadService.UploadImageAsync(baseUrl, "Product", product.ProductId.ToString(), productRequest.Image);
            product.Image = image;

            // Set parameter impacts
            product.SetParameterImpacts(productRequest.ParameterImpacts);

            // Insert the product into the repository and save changes
            _productRepository.Insert(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            productResponse.Status = "201";
            productResponse.Message = "Product created successfully.";

            return productResponse;
        }



        public async Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productRequest, CancellationToken cancellationToken)
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

        public async Task<ProductDetailResponse> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken)
        {
            var product = await _productRepository
                .GetQueryable()
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                   /* .ThenInclude(f => f.Member)*/
                .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

            if (product == null)
                return null;

            return new ProductDetailResponse
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description,
                Image = product.Image,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Brand = product.Brand,
                ManufactureDate = product.ManufactureDate,
                ExpiryDate = product.ExpiryDate,
                ParameterImpactment = product.ParameterImpactment,
                Category = new CategoryInfo
                {
                    CategoryId = product.Category?.CategoryId ?? Guid.Empty,
                    Name = product.Category?.Name
                },
                Shop = new ShopInfo
                {
                    ShopId = product.Shop?.ShopId ?? Guid.Empty,
                    ShopName = product.Shop?.ShopName
                },
                Feedbacks = product.Feedbacks?.Select(f => new FeedbackInfo
                {
                    FeedbackId = f.FeedbackId,
                    /*MemberName = f.Member?.UserName,*/
                    Rate = f.Rate,
                    Content = f.Content
                }).ToList()
            };
        }

    public async Task<IEnumerable<ProductSearchResponse>> SearchProductsAsync(
     string productName,
     string brand,
     string parameterImpact,
     string categoryName,
     CancellationToken cancellationToken)
        {
            var query = _productRepository.GetQueryable();

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

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Include(p => p.Category)
                            .Where(p => p.Category.Name.Contains(categoryName));
            }

            // Project to response model to avoid circular references
            var results = await query
                .Include(p => p.Category)
                .Select(p => new ProductSearchResponse
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Brand = p.Brand,
                    ManufactureDate = p.ManufactureDate,
                    ExpiryDate = p.ExpiryDate,
                    ParameterImpactment = p.ParameterImpactment,
                    Image = p.Image,
                    Category = new CategoryInfo
                    {
                        CategoryId = p.Category.CategoryId,
                        Name = p.Category.Name
                    }
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return results;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            return await _productRepository
                .GetQueryable()
                .AsNoTracking()  // For better performance since we're just reading
                .ToListAsync(cancellationToken);
        }

    }
}

