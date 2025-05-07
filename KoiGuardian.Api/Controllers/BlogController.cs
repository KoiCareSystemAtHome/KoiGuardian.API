using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        public async Task<BlogResponse> CreateBlog([FromBody] BlogRequest createBlog, CancellationToken cancellationToken)
        {
            return await _services.CreateBlogAsync(createBlog, cancellationToken);
        }

        // Endpoint for updating a blog
        [HttpPut("update-blog")]
        [Authorize]
        public async Task<BlogResponse> UpdateBlog([FromBody] BlogUpdateRequest updateBlog, CancellationToken cancellationToken)
        {
            return await _services.UpdateBlogAsync(updateBlog, cancellationToken);
        }

        // Endpoint for getting a blog by ID
        [HttpGet("{blogId}")]
        public async Task<BlogDto> GetBlogById(Guid blogId, CancellationToken cancellationToken)
        {
            return await _services.GetBlogByIdAsync(blogId, cancellationToken);
        }

        // Endpoint for getting all approved blogs
        [HttpGet("approved-blogs")]
        [Authorize]
        public async Task<IList<BlogDto>> GetAllBlogsIsApprovedTrueAsync(CancellationToken cancellationToken)
        {
            return await _services.GetAllBlogsIsApprovedTrueAsync(cancellationToken);
        }

        // Endpoint for getting all unapproved blogs
        [HttpGet("unapproved-blogs")]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<IList<Blog>> GetAllUnapprovedBlogs(CancellationToken cancellationToken)
        {
            return await _services.GetAllBlogsIsApprovedFalseAsync(cancellationToken);
        }

        // Endpoint for getting all blogs
        [HttpGet("all-blogs")]
        public async Task<IList<BlogDto>> GetAllBlogsAsync(CancellationToken cancellationToken)
        {
            return await _services.GetAllBlogsAsync(cancellationToken);
        }

     


        [HttpGet("search")]
        public async Task<ActionResult<IList<BlogDto>>> SearchBlogs(
    [FromQuery] DateTime? startDate,
    [FromQuery] string searchTitle,
    CancellationToken cancellationToken)
        {
            var results = await _services.GetFilteredBlogsAsync(
                startDate,

                searchTitle,
                cancellationToken);

            if (!results.Any())
            {
                return Ok(new { Message = "No blogs found matching the criteria." });
            }

            return Ok(results);
        }

        [HttpPost("blogs/{blogId}/view")]
        public async Task<IActionResult> IncrementBlogView(Guid blogId, CancellationToken cancellationToken)
        {
            var response = await _services.IncrementBlogViewAsync(blogId, cancellationToken);

            if (response.Status == "404")
                return NotFound(response);

            if (response.Status == "500")
                return StatusCode(500, response);

            return Ok(response);
        }

        [HttpGet("tag/{tag}")]
        public async Task<ActionResult<IList<Blog>>> GetBlogsByTag(string tag, CancellationToken cancellationToken)
        {
            var blogs = await _services.GetBlogsByTagAsync(tag, cancellationToken);
            return blogs.Any() ? Ok(blogs) : Ok(new { Message = "No blogs found with this tag." });
        }

        [HttpPut("{blogId}/approve")]
        [Authorize(Roles = ConstantValue.AdminRole)]
        public async Task<IActionResult> ApproveOrRejectBlog(Guid blogId, [FromBody] bool isApproved, CancellationToken cancellationToken)
        {
            var response = await _services.ApproveOrRejectBlogAsync(blogId, isApproved, cancellationToken);
            return response != null ? Ok(response) : NotFound(new { Message = "Blog not found" });
        }


        [HttpPost("report/{blogId}")]
        [Authorize] // Yêu cầu user phải đăng nhập
        public async Task<IActionResult> ReportBlog(Guid blogId, [FromBody] ReportBlogRequest request, CancellationToken cancellationToken)
        {
            var response = await _services.ReportBlogAsync(blogId, request.Reason, cancellationToken);
            if (response.Status == "200")
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        public class ReportBlogRequest
        {
            public string Reason { get; set; }
        }


    }

}
