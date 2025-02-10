using KoiGuardian.Api.Services;
using KoiGuardian.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;

        public ArticlesController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateArticle([FromBody] ArticleRequest request, CancellationToken cancellationToken)
        {
            return StatusCode(201, await _articleService.CreateArticleAsync(request, cancellationToken));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] ArticleUpdateRequest request, CancellationToken cancellationToken)
        {
            request.Id = id;
            return StatusCode(200, await _articleService.UpdateArticleAsync(request, cancellationToken));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetArticle(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _articleService.GetArticleByIdAsync(id, cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllArticles(CancellationToken cancellationToken)
        {
            return Ok(await _articleService.GetAllArticlesAsync(cancellationToken));
        }
    }
}