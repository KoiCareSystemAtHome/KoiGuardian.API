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
using KoiGuardian.Api.Helper;
using MongoDB.Driver;

namespace KoiGuardian.Api.Services
{
    public interface IBlogService
    {
        Task<BlogResponse> CreateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken);
        Task<BlogResponse> UpdateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken);
        Task<BlogDto> GetBlogByIdAsync(Guid blogId, CancellationToken cancellationToken);
        Task<IList<Blog>> GetAllBlogsIsApprovedTrueAsync(CancellationToken cancellationToken);
        Task<IList<Blog>> GetAllBlogsIsApprovedFalseAsync(CancellationToken cancellationToken);

        Task<IList<Blog>> GetBlogsByTagAsync(string tag, CancellationToken cancellationToken);

        Task<BlogResponse> ApproveOrRejectBlogAsync(Guid blogId, bool isApproved, CancellationToken cancellationToken);

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
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        

        public BlogService(
            IRepository<Blog> blogRepository,
            IRepository<BlogProduct> blogProductRepository,
            IRepository<Shop> shopRepository,
            IRepository<User> userRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)

        {
            _blogRepository = blogRepository;
            _blogProductRepository = blogProductRepository;
            _shopRepository = shopRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;

        }

        public async Task<BlogResponse> CreateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();

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
                Tag = "Pending",    // Đặt Tag mặc định là "Pending"
                IsApproved = false, // Chưa được duyệt
                Type = blogRequest.Type,
                ShopId = blogRequest.ShopId,
                ReportedDate = blogRequest.ReportedDate,
                ReportedBy = blogRequest.ReportedBy
            };

            _blogRepository.Insert(blog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new BlogResponse
            {
                Status = "201",
                Message = "Blog created successfully and is pending approval."
            };
        }


        public async Task<BlogDto> GetBlogByIdAsync(Guid blogId, CancellationToken cancellationToken)
        {
            var blog = await _blogRepository
                .GetQueryable()
                .Include(b => b.BlogProducts)
                    .ThenInclude(bp => bp.Product)
                .Include(b => b.Shop)
                .FirstOrDefaultAsync(b => b.BlogId == blogId, cancellationToken);

            if (blog == null)
            {
                return null; // Or throw an exception, depending on your requirements
            }

            return new BlogDto
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
                ShopId = blog.ShopId,
                Shop = blog.Shop != null ? new ShopBasicDto
                {
                    ShopId = blog.Shop.ShopId,
                    Name = blog.Shop.ShopName,
                    Description = blog.Shop.ShopDescription
              
                } : null,
                Products = blog.BlogProducts?.Select(bp => new ProductBasicDto
                {
                    ProductId = bp.Product?.ProductId ?? Guid.Empty, 
                    Name = bp.Product?.ProductName,
                    Price = bp.Product.Price,
                    Image = bp.Product.Image
                }).ToList() ?? new List<ProductBasicDto>()
            };
        }


        public async Task<BlogResponse> UpdateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();
            var existingBlog = await _blogRepository.GetAsync(x => x.BlogId.Equals(blogRequest.BlogId), cancellationToken);

            if (existingBlog == null)
            {
                return new BlogResponse
                {
                    Status = "404",
                    Message = "Blog not found."
                };
            }

            // Update blog properties
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

            // Handle BlogProducts (upsert logic)
            var existingBlogProducts = await _blogProductRepository.FindAsync(bp => bp.BlogId == existingBlog.BlogId, cancellationToken);
            var existingProductIds = existingBlogProducts.Select(bp => bp.ProductId).ToList();
            var newProductIds = blogRequest.ProductIds ?? new List<Guid>();

            // Remove BlogProducts that are no longer in the request
            foreach (var existingBlogProduct in existingBlogProducts)
            {
                if (!newProductIds.Contains(existingBlogProduct.ProductId))
                {
                    _blogProductRepository.Delete(existingBlogProduct);
                }
            }

            // Add new BlogProducts that don't already exist
            foreach (var productId in newProductIds)
            {
                if (!existingProductIds.Contains(productId))
                {
                    var blogProduct = new BlogProduct
                    {
                        BPId = Guid.NewGuid(), // Generate new unique ID
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


        public async Task<BlogResponse> ApproveOrRejectBlogAsync(Guid blogId, bool isApproved, CancellationToken cancellationToken)
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

            blog.IsApproved = isApproved;
            blog.ReportedDate = DateTime.UtcNow;
            blog.Tag = isApproved ? "Approved" : "Rejected";

            _blogRepository.Update(blog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Lấy thông tin Shop
            var shop = await _shopRepository.GetAsync(s => s.ShopId == blog.ShopId, cancellationToken);
            if (shop == null)
            {
                return new BlogResponse
                {
                    Status = "404",
                    Message = "Shop not found."
                };
            }

            // Lấy UserId từ Shop
            var user = await _userRepository.GetAsync(u => u.Id == shop.UserId, cancellationToken);
            if (user == null)
            {
                return new BlogResponse
                {
                    Status = "404",
                    Message = "User not found."
                };
            }

            // Thay đổi cách lấy email giống code mẫu
            string userEmail = user.Email ?? user.Email; // Ưu tiên ContactEmail, nếu null thì lấy Email

            if (string.IsNullOrEmpty(userEmail))
            {
                return new BlogResponse
                {
                    Status = "400",
                    Message = "User email not found."
                };
            }

            // Gửi email cho User
            string subject = isApproved ? "Your blog has been approved!" : "Your blog has been rejected.";
            string body = isApproved ? "Congratulations! Your blog has been approved."
                                     : "Unfortunately, your blog has been rejected.";

            SendMail.SendEmail(userEmail, subject, body, null);

            return new BlogResponse
            {
                Status = "200",
                Message = isApproved ? "Blog approved successfully." : "Blog rejected successfully."
            };
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

        public async Task<IList<Blog>> GetBlogsByTagAsync(string tag, CancellationToken cancellationToken)
        {
            return await _blogRepository.FindAsync(
                b => b.Tag == tag,
                cancellationToken
            );
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