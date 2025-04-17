using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.DataAccess;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using HtmlAgilityPack;


namespace KoiGuardian.Api.Services
{
    public interface IArticleService
    {
        Task<ArticleResponse> CreateArticleAsync(ArticleRequest articleRequest, CancellationToken cancellationToken);
        Task<ArticleResponse> UpdateArticleAsync(ArticleUpdateRequest articleRequest, CancellationToken cancellationToken);
        Task<ArticleResponse> GetArticleByIdAsync(Guid articleId, CancellationToken cancellationToken);
        Task<IEnumerable<Article>> GetAllArticlesAsync(CancellationToken cancellationToken);

    }

    public class ArticleService : IArticleService
    {
        private readonly IRepository<Article> _articleRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;

        public ArticleService(
        IRepository<Article> articleRepository,
        IUnitOfWork<KoiGuardianDbContext> unitOfWork,
        IHttpClientFactory httpClientFactory)
        {
            _articleRepository = articleRepository;
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
        }





        public async Task<ArticleResponse> CreateArticleAsync(ArticleRequest articleRequest, CancellationToken cancellationToken)
        {
            var articleResponse = new ArticleResponse();
            string title = articleRequest.Title;

            // Nếu tiêu đề không được nhập, lấy từ trang web
            if (string.IsNullOrEmpty(title))
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    var response = await httpClient.GetStringAsync(articleRequest.Link);

                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(response);

                    var titleNode = htmlDocument.DocumentNode.SelectSingleNode("//title");
                    if (titleNode != null)
                    {
                        title = titleNode.InnerText.Trim();
                    }
                }
                catch (Exception ex)
                {
                    articleResponse.Status = "500";
                    articleResponse.Message = "Error fetching article title: " + ex.Message;
                    return articleResponse;
                }
            }

            // Kiểm tra nếu tiêu đề không chứa từ "Koi" thì không thêm bài viết
            if (!title.Contains("Koi", StringComparison.OrdinalIgnoreCase))
            {
                articleResponse.Status = "400";
                articleResponse.Message = "Article title must contain the word 'Koi'.";
                return articleResponse;
            }

            var article = new Article
            {
                Link = articleRequest.Link,
                Title = title,
                CrawDate = DateTime.UtcNow,
                isSeen = false
            };

            _articleRepository.Insert(article);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            articleResponse.Status = "201";
            articleResponse.Message = "Article created successfully.";
            articleResponse.Title = title;
            return articleResponse;
        }


        public async Task<ArticleResponse> UpdateArticleAsync(ArticleUpdateRequest articleRequest, CancellationToken cancellationToken)
        {
            var articleResponse = new ArticleResponse();

            var existingArticle = await _articleRepository.GetAsync(x => x.Id == articleRequest.Id, cancellationToken);
            if (existingArticle == null)
            {
                articleResponse.Status = "404";
                articleResponse.Message = "Article with the given ID was not found.";
                return articleResponse;
            }

            existingArticle.Link = articleRequest.Link;
            existingArticle.Title = articleRequest.Title;
            existingArticle.isSeen = articleRequest.IsSeen;

            _articleRepository.Update(existingArticle);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                articleResponse.Status = "200";
                articleResponse.Message = "Article updated successfully.";
            }
            catch (Exception ex)
            {
                articleResponse.Status = "500";
                articleResponse.Message = "Error updating article: " + ex.Message;
            }

            return articleResponse;
        }

        public async Task<ArticleResponse> GetArticleByIdAsync(Guid articleId, CancellationToken cancellationToken)
        {
            var article = await _articleRepository
                .GetQueryable()
                .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

            if (article == null)
                return null;

            // Mark as seen when retrieved
            article.isSeen = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ArticleResponse
            {
                Id = article.Id,
                Link = article.Link,
                Title = article.Title,
                IsSeen = article.isSeen,
                CrawDate = article.CrawDate
            };
        }

        public async Task<IEnumerable<Article>> GetAllArticlesAsync(CancellationToken cancellationToken)
        {
            return await _articleRepository
                .GetQueryable()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }





    }
}
