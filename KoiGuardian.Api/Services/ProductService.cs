using Azure.Core;
using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using static KoiGuardian.Models.Request.FoodRequest;

namespace KoiGuardian.Api.Services
{
    public interface IProductService
    {
        Task<ProductResponse> CreateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken);
        Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productRequest, CancellationToken cancellationToken);
        Task<ProductDetailResponse> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken);

        Task<ProductResponse> CreateFoodAsync( FoodRequest foodRequest, CancellationToken cancellationToken);

        Task<ProductResponse> CreateMedicnieAsync( MedicineRequest medicineRequest, CancellationToken cancellationToken);

        Task<ProductResponse> UpdateMedicineAsync(MedicineUpdateRequest medicineRequest, CancellationToken cancellationToken);

        Task<ProductResponse> UpdateFoodAsync(FoodUpdateRequest foodRequest, CancellationToken cancellationToken);

        Task<IEnumerable<ProductSearchResponse>> GetProductsByTypeAsync(
        ProductType productType,
        CancellationToken cancellationToken,
       
        bool sortDescending = false);

        Task<IEnumerable<ProductSearchResponse>> SearchProductsAsync(
      string productName,
      string brand,
      string parameterImpact,
      string categoryName,
      CancellationToken cancellationToken);

        Task<IEnumerable<ProductGetRequest>> GetAllProductsAsync(CancellationToken cancellationToken);
        Task<IEnumerable<MedicineResponse>> GetAllMedicineAsync(CancellationToken cancellationToken);
        Task<IEnumerable<FoodResponse>> GetAllFoodAsync(CancellationToken cancellationToken);


    }

    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Shop> _shopRepository;
        private readonly IRepository<Food> _foodRepository;
        private readonly IRepository<Medicine> _medicineRepository;
        private readonly IRepository<MedicinePondParameter> _medicinePondParameterRepository;
        private readonly IRepository<PondStandardParam> _pondStandardParamRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        private readonly IImageUploadService _imageUploadService;

        public ProductService(
            IRepository<Product> productRepository,
            IRepository<Shop> shopRepository,
            IRepository<Food> foodRepository,
            IRepository<Medicine> medicineRepository,
            IRepository<MedicinePondParameter> medicinePondParameterRepository,
            IRepository<PondStandardParam> pondStandardParamRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork,
            IImageUploadService imageUpload
            )

        {
            _productRepository = productRepository;
            _shopRepository = shopRepository;
            _foodRepository = foodRepository;
            _medicineRepository = medicineRepository;
            _medicinePondParameterRepository = medicinePondParameterRepository;
            _pondStandardParamRepository = pondStandardParamRepository;
            _unitOfWork = unitOfWork;
            _imageUploadService = imageUpload;
        }

        public async Task<ProductResponse> CreateProductAsync(ProductRequest productRequest, CancellationToken cancellationToken)
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
                Type = (ProductType)Convert.ToInt32(ProductType.Pond_Equipment),
                Image = productRequest.Image,
                ShopId = productRequest.ShopId
            };

            // Upload the image
           
            

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

            // Update basic product information
            existingProduct.ProductName = productRequest.ProductName;
            existingProduct.Description = productRequest.Description;
            existingProduct.Price = productRequest.Price;
            existingProduct.StockQuantity = productRequest.StockQuantity;
            existingProduct.CategoryId = productRequest.CategoryId;
            existingProduct.Brand = productRequest.Brand;
            existingProduct.ManufactureDate = productRequest.ManufactureDate;
            existingProduct.ExpiryDate = productRequest.ExpiryDate;
            existingProduct.ShopId = productRequest.ShopId;
            existingProduct.Image = productRequest.Image;
           
           

            // Update parameter impacts if provided
            if (productRequest.ParameterImpacts != null)
            {
                existingProduct.SetParameterImpacts(productRequest.ParameterImpacts);
            }

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



        public async Task<ProductResponse> UpdateFoodAsync(FoodUpdateRequest foodRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

            var existingProduct = await _productRepository.GetAsync(x => x.ProductId == foodRequest.ProductId, cancellationToken);
            if (existingProduct == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Product with the given ID was not found.";
                return productResponse;
            }

            if (existingProduct.Type != ProductType.Food)
            {
                productResponse.Status = "400";
                productResponse.Message = "The specified product is not a Food.";
                return productResponse;
            }

            var existingFood = await _foodRepository.GetAsync(x => x.ProductId == foodRequest.ProductId, cancellationToken);
            if (existingFood == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Food details not found.";
                return productResponse;
            }

            // Update Product entity fields
            existingProduct.ProductName = foodRequest.ProductName;
            existingProduct.Description = foodRequest.Description;
            existingProduct.Price = foodRequest.Price;
            existingProduct.StockQuantity = foodRequest.StockQuantity;
            existingProduct.CategoryId = foodRequest.CategoryId;
            existingProduct.Brand = foodRequest.Brand;
            existingProduct.ManufactureDate = foodRequest.ManufactureDate;
            existingProduct.ExpiryDate = foodRequest.ExpiryDate;
            existingProduct.Image = foodRequest.Image;
            
           

            // Update parameter impacts on Product if provided
            if (foodRequest.ParameterImpacts != null)
            {
                existingProduct.SetParameterImpacts(foodRequest.ParameterImpacts);
            }

            // Update Food-specific fields
            existingFood.Name = foodRequest.Name;
            existingFood.AgeFrom = foodRequest.AgeFrom;
            existingFood.AgeTo = foodRequest.AgeTo;

            _productRepository.Update(existingProduct);
            _foodRepository.Update(existingFood);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                productResponse.Status = "200";
                productResponse.Message = "Food updated successfully.";
            }
            catch (Exception ex)
            {
                productResponse.Status = "500";
                productResponse.Message = "Error updating food: " + ex.Message;
            }

            return productResponse;
        }

        public async Task<ProductResponse> UpdateMedicineAsync(MedicineUpdateRequest medicineRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

            var existingProduct = await _productRepository.GetAsync(x => x.ProductId == medicineRequest.ProductId, cancellationToken);
            if (existingProduct == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Product with the given ID was not found.";
                return productResponse;
            }

            if (existingProduct.Type != ProductType.Medicine)
            {
                productResponse.Status = "400";
                productResponse.Message = "The specified product is not a Medicine.";
                return productResponse;
            }

            var existingMedicine = await _medicineRepository.GetAsync(x => x.ProductId == medicineRequest.ProductId, cancellationToken);
            if (existingMedicine == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Medicine details not found.";
                return productResponse;
            }

            // Update Product entity fields
            existingProduct.ProductName = medicineRequest.ProductName;
            existingProduct.Description = medicineRequest.Description;
            existingProduct.Price = medicineRequest.Price;
            existingProduct.StockQuantity = medicineRequest.StockQuantity;
            existingProduct.CategoryId = medicineRequest.CategoryId;
            existingProduct.Brand = medicineRequest.Brand;
            existingProduct.ManufactureDate = medicineRequest.ManufactureDate;
            existingProduct.ExpiryDate = medicineRequest.ExpiryDate;
            existingProduct.Image = medicineRequest.Image;
               
            // Update image on Product if provided
            

            // Update parameter impacts on Product if provided
            if (medicineRequest.ParameterImpacts != null)
            {
                existingProduct.SetParameterImpacts(medicineRequest.ParameterImpacts);
            }

            // Update Medicine-specific fields
            existingMedicine.Medicinename = medicineRequest.MedicineName;
            existingMedicine.DosageForm = medicineRequest.DosageForm;
            existingMedicine.Symtomps = medicineRequest.Symptoms;

            _productRepository.Update(existingProduct);
            _medicineRepository.Update(existingMedicine);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                productResponse.Status = "200";
                productResponse.Message = "Medicine updated successfully.";
            }
            catch (Exception ex)
            {
                productResponse.Status = "500";
                productResponse.Message = "Error updating medicine: " + ex.Message;
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
                Type = product.Type,
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
                    Type = p.Type,
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

        public async Task<IEnumerable<ProductGetRequest>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            return await _productRepository
                .GetQueryable()
                .AsNoTracking()  
                .Include(p => p.Shop)    
                .Include(p => p.Category) 
                .Include(p => p.Feedbacks)
                .Select(p => new ProductGetRequest
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Image = p.Image,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name, 
                    Brand = p.Brand,
                    ManufactureDate = p.ManufactureDate,
                    ExpiryDate = p.ExpiryDate,
                    
                    ParameterImpacts = string.IsNullOrEmpty(p.ParameterImpactment)
                        ? new Dictionary<string, ParameterImpactType>()
                        : JsonConvert.DeserializeObject<Dictionary<string, ParameterImpactType>>(p.ParameterImpactment),
                    ShopId = p.ShopId,
                    ShopName = p.Shop.ShopName,
                    FeedbackId = p.Feedbacks.FirstOrDefault() != null ? p.Feedbacks.FirstOrDefault().FeedbackId : Guid.Empty,
                    Content = p.Feedbacks.FirstOrDefault() != null ? p.Feedbacks.FirstOrDefault().Content : null,

                    Type = p.Type
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<ProductResponse> CreateFoodAsync( FoodRequest foodRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

            // Kiểm tra Shop có tồn tại không
            var shop = await _shopRepository.GetAsync(x => x.ShopId == foodRequest.ShopId, cancellationToken);
            if (shop == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Specified shop does not exist.";
                return productResponse;
            }

            // Tạo Product
            var product = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = foodRequest.ProductName,
                Description = foodRequest.Description,
                Price = foodRequest.Price,
                StockQuantity = foodRequest.StockQuantity,
                CategoryId = foodRequest.CategoryId,
                Brand = foodRequest.Brand,
                ManufactureDate = foodRequest.ManufactureDate,
                ExpiryDate = foodRequest.ExpiryDate,
                ShopId = foodRequest.ShopId,
                Image = foodRequest.Image,

                Type = (ProductType)Convert.ToInt32(ProductType.Food) // Đặt loại sản phẩm là Food
            };

            // Upload hình ảnh
           

            product.SetParameterImpacts(foodRequest.ParameterImpacts);
            // Lưu Product vào database
            _productRepository.Insert(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Tạo Food và liên kết với Product
            var food = new Food
            {
                ProductId = product.ProductId,
                Name = foodRequest.Name,
                AgeFrom = foodRequest.AgeFrom,
                AgeTo = foodRequest.AgeTo
            };

            // Lưu Food vào database
            _foodRepository.Insert(food);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            productResponse.Status = "201";
            productResponse.Message = "Food created successfully.";

            return productResponse;
        }

        public async Task<ProductResponse> CreateMedicnieAsync( MedicineRequest medicineRequest, CancellationToken cancellationToken)
        {
            var productResponse = new ProductResponse();

            // Kiểm tra Shop có tồn tại không
            var shop = await _shopRepository.GetAsync(x => x.ShopId == medicineRequest.ShopId, cancellationToken);
            if (shop == null)
            {
                productResponse.Status = "404";
                productResponse.Message = "Specified shop does not exist.";
                return productResponse;
            }

            // Tạo Product
            var product = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = medicineRequest.ProductName,
                Description = medicineRequest.Description,
                Price = medicineRequest.Price,
                StockQuantity = medicineRequest.StockQuantity,
                CategoryId = medicineRequest.CategoryId,
                Brand = medicineRequest.Brand,
                ManufactureDate = medicineRequest.ManufactureDate,
                ExpiryDate = medicineRequest.ExpiryDate,
                ShopId = medicineRequest.ShopId,
                Image = medicineRequest.Image,
                Type = (ProductType)Convert.ToInt32(ProductType.Medicine) // Đặt loại sản phẩm là Medicine
            };

            // Upload hình ảnh
            

            product.SetParameterImpacts(medicineRequest.ParameterImpacts);
            // Lưu Product vào database
            _productRepository.Insert(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            
            var medicine = new Medicine
            {
                ProductId = product.ProductId,
                Medicinename = medicineRequest.MedicineName,
                DosageForm = medicineRequest.DosageForm,
                Symtomps = medicineRequest.Symptoms,
            };

            
            _medicineRepository.Insert(medicine);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (medicineRequest.PondParamId.HasValue)
            {
                // Kiểm tra xem PondParamId có tồn tại trong bảng PondStandardParam không
                var pondParam = await _pondStandardParamRepository.GetAsync(x => x.ParameterID == medicineRequest.PondParamId.Value, cancellationToken);

                if (pondParam != null) // Nếu PondParamId hợp lệ
                {
                    var medicinePondParam = new MedicinePondParameter
                    {
                        MedicinePondParameterId = Guid.NewGuid(),
                        MedicineId = medicine.MedicineId,
                        PondParamId = medicineRequest.PondParamId.Value
                    };

                    _medicinePondParameterRepository.Insert(medicinePondParam);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    productResponse.Status = "400";
                    productResponse.Message = "Invalid PondParamId. It must match an existing PondStandardParam.ParameterID.";
                    return productResponse;
                }
            }

            productResponse.Status = "201";
            productResponse.Message = "Medicine created successfully.";

            return productResponse;
        }

        public async Task<IEnumerable<ProductSearchResponse>> GetProductsByTypeAsync(
        ProductType productType,
        CancellationToken cancellationToken,
        
        bool sortDescending = false
        )
        {
            var query = _productRepository.GetQueryable()
                .Where(p => p.Type == productType)
                .Include(p => p.Category);

            var results = await query
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
                    Type = p.Type,
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

        public async Task<IEnumerable<MedicineResponse>> GetAllMedicineAsync(CancellationToken cancellationToken)
        {
            var query = _productRepository.GetQueryable()
                .Where(p => p.Type == ProductType.Medicine)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Join(_medicineRepository.GetQueryable().Include(m => m.MedicinePondParameters),
                    product => product.ProductId,
                    medicine => medicine.ProductId,
                    (product, medicine) => new { Product = product, Medicine = medicine });

            var results = await query
                .Select(x => new MedicineResponse
                {
                    // Product fields
                    ProductId = x.Product.ProductId,
                    ProductName = x.Product.ProductName,
                    Description = x.Product.Description,
                    Price = x.Product.Price,
                    StockQuantity = x.Product.StockQuantity,
                    Image = x.Product.Image,
                    CategoryId = x.Product.CategoryId,
                    CategoryName = x.Product.Category.Name,
                    Brand = x.Product.Brand,
                    ManufactureDate = x.Product.ManufactureDate,
                    ExpiryDate = x.Product.ExpiryDate,
                    ParameterImpactment = x.Product.ParameterImpactment,
                    ShopId = x.Product.ShopId,
                    ShopName = x.Product.Shop.ShopName,
                    Type = x.Product.Type,

                    // Medicine fields
                    MedicineId = x.Medicine.MedicineId,
                    MedicineName = x.Medicine.Medicinename,
                    DosageForm = x.Medicine.DosageForm,
                    Symptoms = x.Medicine.Symtomps,
                    PondParamId = x.Medicine.MedicinePondParameters.FirstOrDefault() != null ?
                        x.Medicine.MedicinePondParameters.FirstOrDefault().PondParamId : null,

                    // Feedback statistics
                    FeedbackCount = x.Product.Feedbacks.Count(),
                    AverageRating = x.Product.Feedbacks.Any() ? x.Product.Feedbacks.Average(f => f.Rate) : 0,
                    FeedbackId = x.Product.Feedbacks.FirstOrDefault() != null ? x.Product.Feedbacks.FirstOrDefault().FeedbackId : Guid.Empty,
                    Content = x.Product.Feedbacks.FirstOrDefault() != null ? x.Product.Feedbacks.FirstOrDefault().Content : null,
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return results;
        }

        public async Task<IEnumerable<FoodResponse>> GetAllFoodAsync(CancellationToken cancellationToken)
        {
            var query = _productRepository.GetQueryable()
                .Where(p => p.Type == ProductType.Food)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Join(_foodRepository.GetQueryable(),
                    product => product.ProductId,
                    food => food.ProductId,
                    (product, food) => new { Product = product, Food = food });

            var results = await query
                .Select(x => new FoodResponse
                {
                    // Product fields
                    ProductId = x.Product.ProductId,
                    ProductName = x.Product.ProductName,
                    Description = x.Product.Description,
                    Price = x.Product.Price,
                    StockQuantity = x.Product.StockQuantity,
                    Image = x.Product.Image,
                    CategoryId = x.Product.CategoryId,
                    CategoryName = x.Product.Category.Name,
                    Brand = x.Product.Brand,
                    ManufactureDate = x.Product.ManufactureDate,
                    ExpiryDate = x.Product.ExpiryDate,
                    ParameterImpactment = x.Product.ParameterImpactment,
                    ShopId = x.Product.ShopId,
                    ShopName = x.Product.Shop.ShopName,
                    Type = x.Product.Type,

                    // Food fields
                    FoodId = x.Food.FoodId,
                    Name = x.Food.Name,
                    AgeFrom = x.Food.AgeFrom,
                    AgeTo = x.Food.AgeTo,

                    // Feedback statistics
                    FeedbackCount = x.Product.Feedbacks.Count(),
                    AverageRating = x.Product.Feedbacks.Any() ? x.Product.Feedbacks.Average(f => f.Rate) : 0,
                    FeedbackId = x.Product.Feedbacks.FirstOrDefault() != null ? x.Product.Feedbacks.FirstOrDefault().FeedbackId : Guid.Empty,
                    Content = x.Product.Feedbacks.FirstOrDefault() != null ? x.Product.Feedbacks.FirstOrDefault().Content : null,

                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return results;
        }
    }



    }

