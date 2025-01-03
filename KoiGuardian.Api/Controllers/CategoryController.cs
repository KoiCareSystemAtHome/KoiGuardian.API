using KoiGuardian.Api.Services;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _services;


        public CategoryController(ICategoryService services)
        {
            _services = services;
        }


        [HttpPost("create-category")]
        public async Task<CategoryResponse> CreateCategory([FromBody] CategoryRequest createCategory, CancellationToken cancellationToken)
        {
            return await _services.CreateCategoryAsync(createCategory, cancellationToken);
        }


        [HttpPut("update-category")]
        public async Task<CategoryResponse> UpdateCategory([FromBody] CategoryRequest updateCategory, CancellationToken cancellationToken)
        {
            return await _services.UpdateCategoryAsync(updateCategory, cancellationToken);
        }

        [HttpGet("{categoryId}")]
        public async Task<IActionResult> GetCategoryById(Guid categoryId, CancellationToken cancellationToken)
        {
            var category = await _services.GetCategoryByIdAsync(categoryId, cancellationToken);

            return Ok(category);
        }


        [HttpGet("all-categories")]
        public async Task<IList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken)
        {
            return await _services.GetAllCategoriesAsync(cancellationToken);
        }
    }
}
