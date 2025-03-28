using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using static KoiGuardian.Models.Request.FoodRequest;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService services) : ControllerBase
    {

        [HttpGet]
        public async Task<IEnumerable<ProductRequest>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            return await services.GetAllProductsAsync(cancellationToken);
        }

        [HttpPost("create-product")]
        public async Task<ProductResponse> CreateProduct([FromBody] ProductRequest createProduct, CancellationToken cancellationToken)
        {
           
            return await services.CreateProductAsync(createProduct, cancellationToken);
        }

        [HttpPost("create-productfood")]
        public async Task<ProductResponse> CreateFood([FromBody]FoodRequest createFood, CancellationToken cancellationToken)
        {
            
            return await services.CreateFoodAsync( createFood, cancellationToken);
        }

        [HttpPost("create-medicine")]
        public async Task<ProductResponse> CreateMedince([FromBody] MedicineRequest createMedicine, CancellationToken cancellationToken)
        {
           
            return await services.CreateMedicnieAsync(createMedicine, cancellationToken);
        }

        [HttpPut("update-product")]
        public async Task<ProductResponse> UpdateProduct([FromBody] ProductUpdateRequest updateProduct, CancellationToken cancellationToken)
        {
            return await services.UpdateProductAsync(updateProduct, cancellationToken);
        }

        [HttpPut("update-food")]
        public async Task<ProductResponse> UpdateFood([FromBody] FoodUpdateRequest updateFood, CancellationToken cancellationToken)
        {
            return await services.UpdateFoodAsync(updateFood, cancellationToken);
        }

        [HttpPut("update-medicine")]
        public async Task<ProductResponse> UpdateMedicine([FromBody] MedicineUpdateRequest updateMedicine, CancellationToken cancellationToken)
        {
            return await services.UpdateMedicineAsync(updateMedicine, cancellationToken);
        }


        [HttpGet("{productId}")]
        public async Task<ProductDetailResponse> GetProductById(Guid productId, CancellationToken cancellationToken)
        {
             
            return await services.GetProductByIdAsync(productId,cancellationToken);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductDetailResponse>>> SearchProducts(
            [FromQuery] string? productName = null,
            [FromQuery] string? brand = null,
            [FromQuery] string? parameterImpact = null,
            [FromQuery] string? CategoryName = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await services.SearchProductsAsync(
                    productName,
                    brand,
                    parameterImpact,
                    CategoryName,
                    cancellationToken);

                if (!results.Any())
                {
                    return NotFound("No products found matching the search criteria.");
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("by-type/{productType}")]
        public async Task<ActionResult<IEnumerable<ProductSearchResponse>>> GetProductsByType(
            ProductType productType,
            CancellationToken cancellationToken,
           
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var results = await services.GetProductsByTypeAsync(
                    productType,
                    cancellationToken,
                   
                    sortDescending
                   );

                if (!results.Any())
                {
                    return NotFound($"No products found for type {productType}");
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("/all-food")]
        public async Task<IEnumerable<FoodResponse>> GetAllFood(CancellationToken cancellationToken)
        {
            return await services.GetAllFoodAsync(cancellationToken);
        }

        [HttpGet("/all-medicine")]
        public async Task<IEnumerable<MedicineResponse>> GetAllMedicine(CancellationToken cancellationToken)
        {
            return await services.GetAllMedicineAsync(cancellationToken);
        }
    }
}
