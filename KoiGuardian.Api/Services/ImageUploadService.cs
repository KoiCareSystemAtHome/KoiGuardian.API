using Azure;
using KoiGuardian.DataAccess.Db;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace KoiGuardian.Api.Services;

public interface IImageUploadService
{
    Task<string> UploadImageAsync(string baseUrl , string tablename, string id, IFormFile imageFile);
}

public class ImageUploadService : IImageUploadService
{
    public async Task<string> UploadImageAsync(string baseUrl, string tablename, string id, IFormFile imageFile)
    {
        var placeholder = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTXxZR0_1ISIJx_T4oB5-5OJVSNgSMFLe8eCw&s";
        try
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return placeholder;
            }

            string fileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";

            string relativeFolderPath = Path.Combine("wwwroot", "Images", tablename);

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), relativeFolderPath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            //var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            string imageUrl = $"{baseUrl}/Images/{tablename}/{fileName}";

            return imageUrl;
        }
        catch (Exception ex)
        {
            return placeholder;
        }
    }
}
