using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _services;

        // Inject the BlogService via constructor
        public BlogController(IBlogService services)
        {
            _services = services;
        }

        // Endpoint for creating a blog
        [HttpPost("create-blog")]
        public async Task<BlogResponse> CreateBlog([FromBody] BlogRequest createBlog, CancellationToken cancellationToken)
        {
            return await _services.CreateBlogAsync(createBlog, cancellationToken);
        }

        // Endpoint for updating a blog
        [HttpPut("update-blog")]
        public async Task<BlogResponse> UpdateBlog([FromBody] BlogRequest updateBlog, CancellationToken cancellationToken)
        {
            return await _services.UpdateBlogAsync(updateBlog, cancellationToken);
        }

        // Endpoint for getting a blog by ID
        [HttpGet("{blogId}")]
        public async Task<Blog> GetBlogById(string blogId, CancellationToken cancellationToken)
        {
            return await _services.GetBlogByIdAsync(blogId, cancellationToken);
        }

        [HttpGet("blogs/pending")]
        public async Task<IActionResult> GetPendingBlogs(CancellationToken cancellationToken)
        {
            var blogs = await _services.GetAllBlogsIsApprovedFalseAsync(cancellationToken);
            return Ok(blogs);
        }

        [HttpGet("blogs/approved")]
        public async Task<IActionResult> GetApprovedBlogs(CancellationToken cancellationToken)
        {
            var blogs = await _services.GetAllBlogsIsApprovedTrueAsync(cancellationToken);
            return Ok(blogs);
        }

    }
}
