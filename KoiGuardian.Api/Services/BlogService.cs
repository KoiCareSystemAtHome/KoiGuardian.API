using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.Api.Services
{
    public interface IBlogService
    {
        Task<BlogResponse> CreateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken);
        Task<BlogResponse> UpdateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken);
        Task<Blog> GetBlogByIdAsync(Guid blogId, CancellationToken cancellationToken);
        Task<IList<Blog>> GetAllBlogsIsApprovedTrueAsync(CancellationToken cancellationToken);
        Task<IList<Blog>> GetAllBlogsIsApprovedFalseAsync(CancellationToken cancellationToken);

        Task<IList<BlogDto>> GetAllBlogsAsync(CancellationToken cancellationToken);

        Task<IList<BlogDto>> GetFilteredBlogsAsync(
            DateTime? createDate,
            string searchTitle,
            CancellationToken cancellationToken);

         Task<BlogResponse> IncrementBlogViewAsync(Guid blogId, CancellationToken cancellationToken);



    }

    public class BlogService : IBlogService
    {
        private readonly IRepository<Blog> _blogRepository;
        private readonly IRepository<BlogProduct> _blogProductRepository;
        private readonly IRepository<Shop> _shopRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        

        public BlogService(
            IRepository<Blog> blogRepository,
            IRepository<BlogProduct> blogProductRepository,
            IRepository<Shop> shopRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)

        {
            _blogRepository = blogRepository;
            _blogProductRepository = blogProductRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;

        }

        public async Task<BlogResponse> CreateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken)
        {

            var blogResponse = new BlogResponse();


            // Check if ShopId exists
            var shopExists = await _shopRepository.AnyAsync(s => s.ShopId == blogRequest.ShopId, cancellationToken);
            if (!shopExists)
            {
                return new BlogResponse
                {
                    Status = "400",
                    Message = "Shop with the provided ShopId does not exist."
                };
            }

            var blog = new Blog
            {
                
                Title = blogRequest.Title,
                Content = blogRequest.Content,
                Images = blogRequest.Images,
                Tag = blogRequest.Tag,
                IsApproved = false,
                Type = blogRequest.Type,
                ShopId = blogRequest.ShopId,
                ReportedDate = blogRequest.ReportedDate,
                View = 0,
                ReportedBy = blogRequest.ReportedBy

            };

            _blogRepository.Insert(blog);

            if (blogRequest.ProductIds?.Any() == true)
            {
                foreach (var productId in blogRequest.ProductIds)
                {
                    var blogProduct = new BlogProduct
                    {
                        BPId = Guid.NewGuid(),
                        BlogId = blog.BlogId,
                        ProductId = productId
                    };
                    _blogProductRepository.Insert(blogProduct);
                }
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                blogResponse.Status = "201";
                blogResponse.Message = "Blog created successfully.";
            }

            catch (Exception ex)
            {
                return new BlogResponse
                {
                    Status = "500",
                    Message = "Error creating blog: " + ex.Message
                };
            }

            return blogResponse;
        }

        public async Task<Blog> GetBlogByIdAsync(Guid blogId, CancellationToken cancellationToken)
        {
            return await _blogRepository
                .GetQueryable()
                .Include(b => b.BlogProducts) 
                .FirstOrDefaultAsync(b => b.BlogId == blogId, cancellationToken);
        }

        public async Task<BlogResponse> UpdateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();
            var existingBlog = await _blogRepository.GetAsync(x => x.BlogId.Equals(blogRequest.BlogId), cancellationToken);

            existingBlog.Title = blogRequest.Title;
            existingBlog.Content = blogRequest.Content;
            existingBlog.Images = blogRequest.Images;
            existingBlog.Tag = blogRequest.Tag;
            existingBlog.IsApproved = blogRequest.IsApproved;
            existingBlog.Type = blogRequest.Type;
            existingBlog.ShopId = blogRequest.ShopId;
            existingBlog.ReportedDate = blogRequest.ReportedDate;
            existingBlog.ReportedBy = blogRequest.ReportedBy;
           

            _blogRepository.Update(existingBlog);

            // Update blog products
            var existingBlogProducts = existingBlog.BlogProducts.ToList();
            foreach (var existingBlogProduct in existingBlogProducts)
            {
                _blogProductRepository.Delete(existingBlogProduct);
            }

            if (blogRequest.ProductIds?.Any() == true)
            {
                foreach (var productId in blogRequest.ProductIds)
                {
                    var blogProduct = new BlogProduct
                    {
                        BPId = Guid.NewGuid(),
                        BlogId = existingBlog.BlogId,
                        ProductId = productId
                    };
                    _blogProductRepository.Insert(blogProduct);
                }
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                blogResponse.Status = "200";
                blogResponse.Message = "Blog updated successfully.";
            }
            catch (Exception ex)
            {
                return new BlogResponse
                {
                    Status = "500",
                    Message = "Error updating blog: " + ex.Message
                };
            }

            return blogResponse;
        }

        public async Task<IList<Blog>> GetAllBlogsIsApprovedFalseAsync(CancellationToken cancellationToken)
        {
            return await _blogRepository.FindAsync(
                b => b.IsApproved == false,
                cancellationToken
            );
        }

        public async Task<IList<Blog>> GetAllBlogsIsApprovedTrueAsync(CancellationToken cancellationToken)
        {
            return await _blogRepository.FindAsync(
                b => b.IsApproved == true,
                cancellationToken
            );
        }

        public async Task<IList<BlogDto>> GetAllBlogsAsync(CancellationToken cancellationToken)
        {
            var blogs = await _blogRepository.GetQueryable()
                .Include(b => b.BlogProducts)
                    .ThenInclude(bp => bp.Product)
                .Include(b => b.Shop)
                .ToListAsync(cancellationToken);

            return blogs.Select(blog => new BlogDto
            {
                BlogId = blog.BlogId,
                Title = blog.Title,
                Content = blog.Content,
                Images = blog.Images,
                Tag = blog.Tag,
                IsApproved = blog.IsApproved,
                Type = blog.Type,
                ReportedBy = blog.ReportedBy,
                ReportedDate = blog.ReportedDate,
                View = blog.View,
                ShopId = blog.ShopId,
                Shop = blog.Shop != null ? new ShopBasicDto
                {
                    ShopId = blog.Shop.ShopId,
                    Name = blog.Shop.ShopName,
                    Description = blog.Shop.ShopDescription,
                   
                } : null,
                Products = blog.BlogProducts?.Select(bp => new ProductBasicDto
                {
                    ProductId = bp.Product.ProductId,
                    Name = bp.Product.ProductName,
                    Price = bp.Product.Price,
                   
                }).ToList() ?? new List<ProductBasicDto>()
            }).ToList();
        }

        public async Task<IList<BlogDto>> GetFilteredBlogsAsync(
            DateTime? createDate,
            string searchTitle,
            CancellationToken cancellationToken)
        {
            var query = _blogRepository.GetQueryable()
                .Include(b => b.BlogProducts)
                    .ThenInclude(bp => bp.Product)
                .Include(b => b.Shop)
                .AsQueryable();

            // Apply date filter if provided
           /* if (createDate.HasValue)
            {
                query = query.Where(b => b.ReportedDate >= startDate.Value);
            }*/

           

            // Apply title search if provided
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                searchTitle = searchTitle.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(searchTitle));
            }

            var blogs = await query.ToListAsync(cancellationToken);

            return blogs.Select(blog => new BlogDto
            {
                BlogId = blog.BlogId,
                Title = blog.Title,
                Content = blog.Content,
                Images = blog.Images,
                Tag = blog.Tag,
                IsApproved = blog.IsApproved,
                Type = blog.Type,
                ReportedBy = blog.ReportedBy,
                ReportedDate = blog.ReportedDate,
                View = blog.View,
                ShopId = blog.ShopId,
                Shop = blog.Shop != null ? new ShopBasicDto
                {
                    ShopId = blog.Shop.ShopId,
                    Name = blog.Shop.ShopName,
                    Description = blog.Shop.ShopDescription,
                } : null,
                Products = blog.BlogProducts?.Select(bp => new ProductBasicDto
                {
                    ProductId = bp.Product.ProductId,
                    Name = bp.Product.ProductName,
                    Price = bp.Product.Price,
                }).ToList() ?? new List<ProductBasicDto>()
            }).ToList();
        }

        public async Task<BlogResponse> IncrementBlogViewAsync(Guid blogId, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();

            try
            {
                var blog = await _blogRepository.GetAsync(x => x.BlogId == blogId, cancellationToken);

                if (blog == null)
                {
                    return new BlogResponse
                    {
                        Status = "404",
                        Message = "Blog not found."
                    };
                }

                // Increment the view count
                blog.View += 1;

                // Update the blog in the repository
                _blogRepository.Update(blog);

                // Save changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                blogResponse.Status = "200";
                blogResponse.Message = "Blog view count incremented successfully.";
            }
            catch (Exception ex)
            {
                return new BlogResponse
                {
                    Status = "500",
                    Message = "Error incrementing blog view count: " + ex.Message
                };
            }

            return blogResponse;
        }




    }




}