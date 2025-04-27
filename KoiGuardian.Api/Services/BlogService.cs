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
        Task<BlogResponse> UpdateBlogAsync(BlogUpdateRequest blogUpdateRequest, CancellationToken cancellationToken);
        Task<BlogDto> GetBlogByIdAsync(Guid blogId, CancellationToken cancellationToken);
        Task<IList<BlogDto>> GetAllBlogsIsApprovedTrueAsync(CancellationToken cancellationToken);
        Task<IList<Blog>> GetAllBlogsIsApprovedFalseAsync(CancellationToken cancellationToken);

        Task<IList<Blog>> GetBlogsByTagAsync(string tag, CancellationToken cancellationToken);

        Task<BlogResponse> ApproveOrRejectBlogAsync(Guid blogId, bool isApproved, CancellationToken cancellationToken);

        Task<IList<BlogDto>> GetAllBlogsAsync(CancellationToken cancellationToken);

        Task<IList<BlogDto>> GetFilteredBlogsAsync(
            DateTime? createDate,
            string searchTitle,
            CancellationToken cancellationToken);

         Task<BlogResponse> IncrementBlogViewAsync(Guid blogId, CancellationToken cancellationToken);

        Task<BlogResponse> ReportBlogAsync(Guid blogId, string reason, CancellationToken cancellationToken);



    }

    public class BlogService : IBlogService
    {
        private readonly IRepository<Blog> _blogRepository;
        private readonly IRepository<BlogProduct> _blogProductRepository;
        private readonly IRepository<Shop> _shopRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;
        

        public BlogService(
            IRepository<Blog> blogRepository,
            IRepository<BlogProduct> blogProductRepository,
            IRepository<Shop> shopRepository,
            IRepository<User> userRepository,
            IRepository<Product> productRepository,
            ICurrentUser currentUser,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)

        {
            _blogRepository = blogRepository;
            _blogProductRepository = blogProductRepository;
            _shopRepository = shopRepository;
            _userRepository = userRepository;
            _productRepository = productRepository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;

        }

        public async Task<BlogResponse> CreateBlogAsync(BlogRequest blogRequest, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();

            // Check if Shop exists
            var shopExists = await _shopRepository.AnyAsync(s => s.ShopId == blogRequest.ShopId, cancellationToken);
            if (!shopExists)
            {
                return new BlogResponse
                {
                    Status = "400",
                    Message = "Shop không tồn tại"
                };
            }

            // Validate that all ProductIds belong to the specified ShopId
            if (blogRequest.ProductIds != null && blogRequest.ProductIds.Any())
            {
                var invalidProducts = await _productRepository
                    .AnyAsync(p => blogRequest.ProductIds.Contains(p.ProductId) && p.ShopId != blogRequest.ShopId, cancellationToken);

                if (invalidProducts)
                {
                    return new BlogResponse
                    {
                        Status = "400",
                        Message = "Sản phẩm không thuộc shop này"
                    };
                }
            }

            // Create new Blog
            // Create new Blog
            var blog = new Blog
            {
                BlogId = Guid.NewGuid(),
                Title = blogRequest.Title,
                Content = blogRequest.Content,
                Images = blogRequest.Images,
                Tag = "Pending",
                IsApproved = null, // set null here
                Type = blogRequest.Type,
                ShopId = blogRequest.ShopId,
            };


            // Insert Blog into repository
            _blogRepository.Insert(blog);

            // Process BlogProduct if ProductIds are provided
            if (blogRequest.ProductIds != null && blogRequest.ProductIds.Any())
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

            return new BlogResponse
            {
                Status = "200",
                Message = "Tạo bài viết thành công.",
               
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
                return null;
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
                Products = blog.BlogProducts != null ? blog.BlogProducts.Select(bp => new ProductBasicDto
                {
                    ProductId = bp.Product != null ? bp.Product.ProductId : Guid.Empty,
                    Name = bp.Product != null ? bp.Product.ProductName : null,
                    Price = bp.Product != null ? bp.Product.Price : 0,
                    Image = bp.Product != null ? bp.Product.Image : null
                }).ToList() : new List<ProductBasicDto>()
            };
        }


        public async Task<BlogResponse> UpdateBlogAsync(BlogUpdateRequest blogUpdateRequest, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();

            if (blogUpdateRequest == null || blogUpdateRequest.BlogId == Guid.Empty)
            {
                return new BlogResponse
                {
                    Status = "400",
                    Message = "Invalid blog update request or missing BlogId."
                };
            }

            try
            {
                var existingBlog = await _blogRepository.GetAsync(x => x.BlogId.Equals(blogUpdateRequest.BlogId), cancellationToken);
                if (existingBlog == null)
                {
                    return new BlogResponse
                    {
                        Status = "404",
                        Message = "Blog not found."
                    };
                }

                if (blogUpdateRequest.ShopId != Guid.Empty && blogUpdateRequest.ShopId != existingBlog.ShopId)
                {
                    var shopExists = await _shopRepository.AnyAsync(s => s.ShopId == blogUpdateRequest.ShopId, cancellationToken);
                    if (!shopExists)
                    {
                        return new BlogResponse
                        {
                            Status = "400",
                            Message = "Shop with the provided ShopId does not exist."
                        };
                    }
                }

                // Update only provided fields
                existingBlog.Title = !string.IsNullOrEmpty(blogUpdateRequest.Title) ? blogUpdateRequest.Title : existingBlog.Title;
                existingBlog.Content = !string.IsNullOrEmpty(blogUpdateRequest.Content) ? blogUpdateRequest.Content : existingBlog.Content;
                existingBlog.Images = blogUpdateRequest.Images ?? existingBlog.Images;
                existingBlog.Tag = "Pending";
                existingBlog.IsApproved = null;
                existingBlog.Type = !string.IsNullOrEmpty(blogUpdateRequest.Type) ? blogUpdateRequest.Type : existingBlog.Type;
                existingBlog.ShopId = blogUpdateRequest.ShopId != Guid.Empty ? blogUpdateRequest.ShopId : existingBlog.ShopId;

                // Ensure ReportedDate is UTC if it exists
                if (existingBlog.ReportedDate.HasValue)
                {
                    existingBlog.ReportedDate = DateTime.SpecifyKind(existingBlog.ReportedDate.Value, DateTimeKind.Utc);
                }

                _blogRepository.Update(existingBlog);

                if (blogUpdateRequest.ProductIds != null)
                {
                    var existingBlogProducts = await _blogProductRepository.FindAsync(bp => bp.BlogId == existingBlog.BlogId, cancellationToken);
                    var existingProductIds = existingBlogProducts
                        .Where(bp => bp.Product != null)
                        .Select(bp => bp.ProductId)
                        .ToList();
                    var newProductIds = blogUpdateRequest.ProductIds;

                    foreach (var existingBlogProduct in existingBlogProducts)
                    {
                        if (!newProductIds.Contains(existingBlogProduct.ProductId))
                        {
                            _blogProductRepository.Delete(existingBlogProduct);
                        }
                    }

                    foreach (var productId in newProductIds)
                    {
                        if (!existingProductIds.Contains(productId))
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
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new BlogResponse
                {
                    Status = "200",
                    Message = "Blog updated successfully."
                };
            }
            catch (DbUpdateException dbEx)
            {
                return new BlogResponse
                {
                    Status = "500",
                    Message = $"Database error updating blog: {dbEx.InnerException?.Message ?? dbEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new BlogResponse
                {
                    Status = "500",
                    Message = $"Error updating blog: {ex.Message}"
                };
            }
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

        public async Task<IList<BlogDto>> GetAllBlogsIsApprovedTrueAsync(CancellationToken cancellationToken)
        {
            var blogs = await _blogRepository.GetQueryable()
                .Where(b => b.IsApproved == true)
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
                IsApproved = blog.IsApproved ?? false,
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
                    Image = bp.Product?.Image
                }).ToList() ?? new List<ProductBasicDto>()
            }).ToList();
        }

        public async Task<IList<BlogDto>> GetAllBlogsAsync(CancellationToken cancellationToken)
        {
            var blogs = await _blogRepository.GetQueryable()
                .Include(b => b.BlogProducts)
                    .ThenInclude(bp => bp.Product)
                .Include(b => b.Shop)
                .Select(blog => new BlogDto
                {
                    BlogId = blog.BlogId,
                    Title = blog.Title,
                    Content = blog.Content,
                    Images = blog.Images,
                    Tag = blog.Tag,
                    IsApproved = blog.IsApproved, // Preserve null value
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
                    Products = blog.BlogProducts != null ? blog.BlogProducts.Select(bp => new ProductBasicDto
                    {
                        ProductId = bp.Product != null ? bp.Product.ProductId : Guid.Empty,
                        Name = bp.Product != null ? bp.Product.ProductName : null,
                        Price = bp.Product != null ? bp.Product.Price : 0,
                        Image = bp.Product != null ? bp.Product.Image : null
                    }).ToList() : new List<ProductBasicDto>()
                })
                .ToListAsync(cancellationToken);

            return blogs;
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

        public async Task<BlogResponse> ReportBlogAsync(Guid blogId, string reason, CancellationToken cancellationToken)
        {
            var blogResponse = new BlogResponse();

            // Kiểm tra xem blog có tồn tại không
            var blog = await _blogRepository.GetAsync(x => x.BlogId == blogId, cancellationToken);
            if (blog == null)
            {
                return new BlogResponse
                {
                    Status = "404",
                    Message = "Blog not found."
                };
            }

            // Kiểm tra lý do report có hợp lệ không
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new BlogResponse
                {
                    Status = "400",
                    Message = "Reason for reporting is required."
                };
            }

            // Lấy thông tin user từ ICurrentUser
            var reportedBy = _currentUser.UserName();
            if (string.IsNullOrEmpty(reportedBy))
            {
                return new BlogResponse
                {
                    Status = "401",
                    Message = "User not authenticated."
                };
            }

            // Cập nhật thông tin báo cáo
            blog.ReportedBy = reportedBy; 
            blog.ReportedDate = DateTime.UtcNow; 
            blog.Tag = "Reported"; 
            blog.IsApproved = true; 

            // Lưu thay đổi vào database
            _blogRepository.Update(blog);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Gửi email thông báo đến admin
                string adminEmail = "admin@koiguardian.com"; // Thay bằng email thật
                string subject = $"Blog Reported: {blog.Title}";
                string body = $"Blog ID: {blog.BlogId}\n" +
                              $"Title: {blog.Title}\n" +
                              $"Reported By: {reportedBy}\n" +
                              $"Reason: {reason}\n" +
                              $"Reported Date: {DateTime.UtcNow}\n" +
                              "Please review this blog.";

                SendMail.SendEmail(adminEmail, subject, body, null);

                blogResponse.Status = "200";
                blogResponse.Message = "Blog has been reported successfully and is under review.";
            }
            catch (Exception ex)
            {
                return new BlogResponse
                {
                    Status = "500",
                    Message = "Error reporting blog: " + ex.Message
                };
            }

            return blogResponse;
        }




    }




}