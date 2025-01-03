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
        [HttpPost("create-product")]
        public async Task<ProductResponse> CreateProduct([FromBody] ProductRequest createProduct, CancellationToken cancellationToken)
        {
            return await services.CreateProductAsync(createProduct, cancellationToken);
        }

        [HttpPut("update-product")]
        public async Task<ProductResponse> UpdateProduct([FromBody] ProductRequest updateProduct, CancellationToken cancellationToken)
        {
            return await services.UpdateProductAsync(updateProduct, cancellationToken);
        }

        [HttpGet("{productId}")]
        public async Task<Product> GetProductById(Guid productId, CancellationToken cancellationToken)
        {
             
            return await services.GetProductByIdAsync(productId, cancellationToken);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(
            [FromQuery] string? productName = null,
            [FromQuery] string? brand = null,
            [FromQuery] string? parameterImpact = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await services.SearchProductsAsync(
                    productName,
                    brand,
                    parameterImpact,
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
