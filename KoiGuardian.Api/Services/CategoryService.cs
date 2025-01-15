using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Services
{
    public interface ICategoryService
    {
        Task<CategoryResponse> CreateCategoryAsync(CategoryRequest categoryRequest, CancellationToken cancellationToken);
        Task<CategoryResponse> UpdateCategoryAsync(CategoryRequest categoryRequest, CancellationToken cancellationToken);
        Task<CategoryRequest> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken);
        Task<IList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IUnitOfWork<KoiGuardianDbContext> _unitOfWork;

        public CategoryService(
            IRepository<Category> categoryRepository,
            IUnitOfWork<KoiGuardianDbContext> unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryResponse> CreateCategoryAsync(CategoryRequest categoryRequest, CancellationToken cancellationToken)
        {
            var categoryResponse = new CategoryResponse();

            // Check if the category already exists
            var existingCategory = await _categoryRepository.GetAsync(x => x.Name == categoryRequest.Name, cancellationToken);
            if (existingCategory != null)
            {
                categoryResponse.Status = "409";
                categoryResponse.Message = "Category with the given name already exists.";
                return categoryResponse;
            }

            var category = new Category
            {
               
                Name = categoryRequest.Name,
                Description = categoryRequest.Description,
                ShopId = categoryRequest.ShopId
            };

            _categoryRepository.Insert(category);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                categoryResponse.Status = "201";
                categoryResponse.Message = "Category created successfully.";
                
            }
            catch (Exception ex)
            {
                categoryResponse.Status = "500";
                categoryResponse.Message = "Error creating category: " + ex.Message;
            }

            return categoryResponse;
        }

        public async Task<CategoryResponse> UpdateCategoryAsync(CategoryRequest categoryRequest, CancellationToken cancellationToken)
        {
            var categoryResponse = new CategoryResponse();

            var existingCategory = await _categoryRepository.GetAsync(x => x.CategoryId == categoryRequest.CategoryId, cancellationToken);
            if (existingCategory == null)
            {
                categoryResponse.Status = "404";
                categoryResponse.Message = "Category not found.";
                return categoryResponse;
            }

            existingCategory.Name = categoryRequest.Name;
            existingCategory.Description = categoryRequest.Description;

            _categoryRepository.Update(existingCategory);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                categoryResponse.Status = "200";
                categoryResponse.Message = "Category updated successfully.";
                
            }
            catch (Exception ex)
            {
                categoryResponse.Status = "500";
                categoryResponse.Message = "Error updating category: " + ex.Message;
            }

            return categoryResponse;
        }

        public async Task<CategoryRequest> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository
                .GetQueryable()
                .Include(p => p.Products)
                .Include(s => s.Shop)
                .FirstOrDefaultAsync(b => b.CategoryId == categoryId, cancellationToken);

            if (category == null)
            {
                return null; // or throw an exception depending on your business logic
            }

            // Mapping Category entity to CategoryRequest model
            var categoryRequest = new CategoryRequest
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                ShopId = category.Shop.ShopId
            };

            return categoryRequest;
        }



        public async Task<IList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken)
        {
            return await _categoryRepository.GetQueryable().ToListAsync(cancellationToken);
        }
    }
}