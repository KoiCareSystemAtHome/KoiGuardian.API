using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService services) : ControllerBase
    {

        [HttpGet]
        public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            return await services.GetAllProductsAsync(cancellationToken);
        }

        [HttpPost("create-product")]
        public async Task<ProductResponse> CreateProduct( ProductRequest createProduct, CancellationToken cancellationToken)
        {
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            return await services.CreateProductAsync(baseUrl,createProduct, cancellationToken);
        }

        [HttpPut("update-product")]
        public async Task<ProductResponse> UpdateProduct( ProductRequest updateProduct, CancellationToken cancellationToken)
        {
            return await services.UpdateProductAsync(updateProduct, cancellationToken);
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
    }
}
