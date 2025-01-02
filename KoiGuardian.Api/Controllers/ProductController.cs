using KoiGuardian.Api.Services;
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
        public async Task<ProductResponse> GetProductById(Guid productId, CancellationToken cancellationToken)
        {
            return await services.GetProductByIdAsync(productId, cancellationToken);
        }
    }
}
